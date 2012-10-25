using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;
//using SaaS.Client;
using DDDSample.Engine;
using DDDSample;

public sealed class Setup
{

    string[] _serviceQueues;
        

    public Setup ConfigureQueues(int serviceQueueCount, int adapterQueueCount)
    {
        _serviceQueues = Enumerable
            .Range(0, serviceQueueCount)
            .Select((s, i) => Conventions.Prefix + "-handle-cmd-service-" + i)
            .ToArray();
        return this;
    }


    private static readonly EnvelopeStreamer Streamer = Contracts.CreateStreamer();
    private static readonly IDocumentStrategy ViewStrategy = new ViewStrategy();
    private static readonly IDocumentStrategy DocStrategy = new DocumentStrategy();


    private IStreamRoot Streaming;
    private IList<Type> boundedContexts = new List<Type>();
    private Func<string, IQueueWriter> CreateQueueWriter;
    private Func<string, IPartitionInbox> CreateInbox;
    private Func<string, IAppendOnlyStore> CreateTapes;
    private Func<IDocumentStrategy, IDocumentStore> CreateDocs;

    public Setup RegisterBoundedContext(Type context)
    {
        boundedContexts.Add(context);
        return this;
    }
    public Setup ConfigStreaming(IStreamRoot streaming)
    {
        this.Streaming = streaming;
        return this;
    }
    public Setup ConfigCreateDocs(Func<IDocumentStrategy,IDocumentStore> createDocs)
    {
        this.CreateDocs = createDocs;
        return this;
    }
    public Setup ConfigInbox(Func<string, IPartitionInbox> createInbox)
    {
        this.CreateInbox = createInbox;
        return this;
    }
    public Setup ConfigTapes(Func<string, IAppendOnlyStore> createTapes)
    {
        this.CreateTapes = createTapes;
        return this;
    }
    public Setup ConfigDocumentStore(Func<IDocumentStrategy, IDocumentStore> createDocs)
    {
        this.CreateDocs = createDocs;
        return this;
    }
    public Setup ConfigQueueWriter(Func<string, IQueueWriter> createQueueWriter)
    {
        this.CreateQueueWriter = createQueueWriter;
        return this;
    }
    public Container Build()
    {
        var appendOnlyStore = CreateTapes(Containers.TapesContainer); 
        var messageStore = new MessageStore(appendOnlyStore, Streamer.MessageSerializer);

        var toCommandRouter = new MessageSender(Streamer, CreateQueueWriter(Queues.RouterQueue));
        var toFunctionalRecorder = new MessageSender(Streamer, CreateQueueWriter(Queues.FunctionalRecorderQueue));
        var toEventHandlers = new MessageSender(Streamer, CreateQueueWriter(Queues.EventProcessingQueue));

        var sender = new TypedMessageSender(toCommandRouter, toFunctionalRecorder);

        var store = new EventStore(messageStore);

        var quarantine = new EnvelopeQuarantine(Streamer, sender, Streaming.GetContainer(Containers.ErrorsContainer));

        var builder = new FluentCqrsEngineBuilder(Streamer, quarantine);

        var events = new RedirectToDynamicEvent();
        var commands = new RedirectToCommand();
        var funcs = new RedirectToCommand();


        builder.
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(events, aem), name: "watch", queues: Queues.EventProcessingQueue).
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(commands, aem), queues: Queues.AggregateHandlerQueue).
            Handle(inbox: CreateInbox, lambda: MakeRouter(messageStore), name: "watch", queues: Queues.RouterQueue).
            Handle(inbox: CreateInbox, lambda: aem => RecordFunctionalEvent(aem, messageStore), queues: Queues.FunctionalRecorderQueue).
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(funcs, aem), queues: _serviceQueues);

        // multiple service queues

        var viewDocs = CreateDocs(ViewStrategy);
        var stateDocs = new NuclearStorage(CreateDocs(DocStrategy));

        //var vector = new DomainIdentityGenerator(stateDocs);

        //var ops = new StreamOps(Streaming);
        var projections = new ProjectionsConsumingOneBoundedContext();
        // Domain Bounded Context

        foreach (var contextType in this.boundedContexts)
        {
            object[] dependencies = new object[6] {sender, viewDocs, commands, store, events, funcs};

            var context = Activator.CreateInstance( contextType, dependencies) as IBoundedContext;

            //IBoundedContext context = new BoundedContext(sender: sender,
            //                                         documentStore: viewDocs,
            //                                         commands: commands,
            //                                         eventStore: store,
            //                                         events: events,
            //                                         funcs: funcs);
            context.Build();

            projections.RegisterFactory(context.Projections);
        }
        


        // wire all projections
        //projections.BuildFor(viewDocs).ForEach(events.WireToWhen);
        projections.BuildFor(viewDocs).ForEach(events.WireToWhen);

        // wire in event store publisher
        var publisher = new MessageStorePublisher(messageStore, toEventHandlers, stateDocs, DoWePublishThisRecord);
        builder.AddTask(c => Task.Factory.StartNew(() => publisher.Run(c)));

        return new Container
        {
            Builder = builder.Instance,
            Setup = this,
            SendToCommandRouter = toCommandRouter,
            MessageStore = messageStore,
            ProjectionFactories = projections,
            ViewDocs = viewDocs,
            Publisher = publisher,
            AppendOnlyStore = appendOnlyStore
        };
    }

    static bool DoWePublishThisRecord(StoreRecord storeRecord)
    {
        return storeRecord.Key != "audit";
    }

    static void RecordFunctionalEvent(ImmutableEnvelope envelope, MessageStore store)
    {
        if (envelope.Message is IFuncEvent) store.RecordMessage("func", envelope);
        else throw new InvalidOperationException("Non-func event {0} landed to queue for tracking stateless events");
    }

    Action<ImmutableEnvelope> MakeRouter(MessageStore tape)
    {
        var entities = CreateQueueWriter(Queues.AggregateHandlerQueue);
        var processing = _serviceQueues.Select(CreateQueueWriter).ToArray();
            
        return envelope =>
        {
            var message = envelope.Message;
            if (message is ICommand)
            {
                // all commands are recorded to audit stream, as they go through router
                tape.RecordMessage("audit", envelope);
            }

            if (message is IEvent)
            {
                throw new InvalidOperationException("Events are not expected in command router queue");
            }

            var data = Streamer.SaveEnvelopeData(envelope);

            if (message is ICommand<IIdentity>)
            {
                entities.PutMessage(data);
                return;
            }
            if (message is IFuncCommand)
            {
                // randomly distribute between queues
                var i = Environment.TickCount % processing.Length;
                processing[i].PutMessage(data);
                return;
            }
            throw new InvalidOperationException("Unknown message format");
        };
    }



    /// <summary>
    /// Helper class that merely makes the concept explicit
    /// </summary>
    public sealed class ProjectionsConsumingOneBoundedContext
    {
        public delegate IEnumerable<object> FactoryForWhenProjections(IDocumentStore store);
        //public delegate IEnumerable<object> FactoryForWhenProjections();

        readonly IList<FactoryForWhenProjections> _factories = new List<FactoryForWhenProjections>();

        public void RegisterFactory(FactoryForWhenProjections factory)
        {
            _factories.Add(factory);
        }

        //public IEnumerable<object> BuildFor(IDocumentStore store)
        //{
        //    return _factories.SelectMany(factory => factory(store));
        //}
        public IEnumerable<object> BuildFor(IDocumentStore store)
        {
            return _factories.SelectMany(factory => factory(store));
        }
    }

    static void CallHandlers(RedirectToDynamicEvent functions, ImmutableEnvelope aem)
    {
        var e = aem.Message as IEvent;

        if (e != null)
        {
            functions.InvokeEvent(e);
        }
    }

    static void CallHandlers(RedirectToCommand serviceCommands, ImmutableEnvelope aem)
    {

        var content = aem.Message;
        var watch = Stopwatch.StartNew();
        serviceCommands.Invoke(content);
        watch.Stop();

        var seconds = watch.Elapsed.TotalSeconds;
        if (seconds > 10)
        {
            SystemObserver.Notify("[Warn]: {0} took {1:0.0} seconds", content.GetType().Name, seconds);
        }
    }
}

public sealed class Container : IDisposable
{
    public Setup Setup;
    public CqrsEngineBuilder Builder;
    public MessageSender SendToCommandRouter;
    public MessageStore MessageStore;
    public IAppendOnlyStore AppendOnlyStore;
    public IDocumentStore ViewDocs;
    public Setup.ProjectionsConsumingOneBoundedContext ProjectionFactories;
    public MessageStorePublisher Publisher;

    public CqrsEngineHost BuildEngine(CancellationToken token)
    {
        return Builder.Build(token);
    }

    public void ExecuteStartupTasks(CancellationToken token)
    {
        Publisher.VerifyEventStreamSanity();

        // we run S2 projections from 3 different BCs against one domain log
        //StartupProjectionRebuilder.Rebuild(
        //    token,
        //    ViewDocs,
        //    MessageStore,
        //    store => ProjectionFactories.BuildFor(store));
        StartupProjectionRebuilder.Rebuild(
            token,
            ViewDocs,
            MessageStore,
            store => ProjectionFactories.BuildFor(store));
    }

    public void Dispose()
    {
        using (AppendOnlyStore)
        {
            AppendOnlyStore = null;
        }
    }
}

public static class ExtendArrayEvil
{
    public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
    {
        foreach (var variable in self)
        {
            action(variable);
        }
    }
}
