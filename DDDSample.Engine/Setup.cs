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
    static readonly string FunctionalRecorderQueue = Conventions.FunctionalEventRecorderQueue;
    static readonly string RouterQueue = Conventions.DefaultRouterQueue;
    static readonly string ErrorsContainer = Conventions.DefaultErrorsFolder;

    const string EventProcessingQueue = Conventions.Prefix + "-handle-events";
    const string AggregateHandlerQueue = Conventions.Prefix + "-handle-cmd-entity";
    string[] _serviceQueues;
        

    public void ConfigureQueues(int serviceQueueCount, int adapterQueueCount)
    {
        _serviceQueues = Enumerable
            .Range(0, serviceQueueCount)
            .Select((s, i) => Conventions.Prefix + "-handle-cmd-service-" + i)
            .ToArray();
    }

    public const string TapesContainer = Conventions.Prefix + "-tapes";

    public static readonly EnvelopeStreamer Streamer = Contracts.CreateStreamer();
    public static readonly IDocumentStrategy ViewStrategy = new ViewStrategy();
    public static readonly IDocumentStrategy DocStrategy = new DocumentStrategy();

    public IStreamRoot Streaming;

    public Func<string, IQueueWriter> CreateQueueWriter;
    public Func<string, IPartitionInbox> CreateInbox;
    public Func<string, IAppendOnlyStore> CreateTapes;
    public Func<IDocumentStrategy, IDocumentStore> CreateDocs;

    public Container Build()
    {
        var appendOnlyStore = CreateTapes(TapesContainer);
        var messageStore = new MessageStore(appendOnlyStore, Streamer.MessageSerializer);

        var toCommandRouter = new MessageSender(Streamer, CreateQueueWriter(RouterQueue));
        var toFunctionalRecorder = new MessageSender(Streamer, CreateQueueWriter(FunctionalRecorderQueue));
        var toEventHandlers = new MessageSender(Streamer, CreateQueueWriter(EventProcessingQueue));

        var sender = new TypedMessageSender(toCommandRouter, toFunctionalRecorder);

        var store = new EventStore(messageStore);

        var quarantine = new EnvelopeQuarantine(Streamer, sender, Streaming.GetContainer(ErrorsContainer));

        var builder = new FluentCqrsEngineBuilder(Streamer, quarantine);

        var events = new RedirectToDynamicEvent();
        var commands = new RedirectToCommand();
        var funcs = new RedirectToCommand();


        builder.
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(events, aem), name: "watch",queues: EventProcessingQueue).
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(commands, aem), queues: AggregateHandlerQueue).
            Handle(inbox: CreateInbox, lambda: MakeRouter(messageStore), name: "watch",queues: RouterQueue).
            Handle(inbox: CreateInbox, lambda: aem => RecordFunctionalEvent(aem, messageStore), queues: FunctionalRecorderQueue).
            Handle(inbox: CreateInbox, lambda: aem => CallHandlers(funcs, aem), queues: _serviceQueues);

        // multiple service queues

        var viewDocs = CreateDocs(ViewStrategy);
        var stateDocs = new NuclearStorage(CreateDocs(DocStrategy));

        //var vector = new DomainIdentityGenerator(stateDocs);

        //var ops = new StreamOps(Streaming);
        var projections = new ProjectionsConsumingOneBoundedContext();
        // Domain Bounded Context
        IBoundedContext context = new BoundedContext(sender: sender, 
                                                     documentStore: viewDocs, 
                                                     command: commands, 
                                                     eventStore: store);
        context.Build();

        projections.RegisterFactory(context.Projections);


        // wire all projections
        //projections.BuildFor(viewDocs).ForEach(events.WireToWhen);
        projections.BuildFor().ForEach(events.WireToWhen);

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
        var entities = CreateQueueWriter(AggregateHandlerQueue);
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
        //public delegate IEnumerable<object> FactoryForWhenProjections(IDocumentStore store);
        public delegate IEnumerable<object> FactoryForWhenProjections();

        readonly IList<FactoryForWhenProjections> _factories = new List<FactoryForWhenProjections>();

        public void RegisterFactory(FactoryForWhenProjections factory)
        {
            _factories.Add(factory);
        }

        //public IEnumerable<object> BuildFor(IDocumentStore store)
        //{
        //    return _factories.SelectMany(factory => factory(store));
        //}
        public IEnumerable<object> BuildFor()
        {
            return _factories.SelectMany(factory => factory());
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
            store => ProjectionFactories.BuildFor());
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
