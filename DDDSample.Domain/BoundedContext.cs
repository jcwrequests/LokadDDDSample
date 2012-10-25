using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample
{
    public class BoundedContext : IBoundedContext
    {
        TypedMessageSender sender;
        IDocumentStore documentStore;
        RedirectToCommand command;
        IEventStore eventStore;

        public BoundedContext(TypedMessageSender sender, 
                              IDocumentStore documentStore,
                              RedirectToCommand commands,
                              IEventStore eventStore,
                              RedirectToDynamicEvent events,
                              RedirectToCommand funcs)
        {
            if (sender == null) throw new ArgumentNullException("sender");
            if (documentStore == null) throw new ArgumentNullException("store");
            if (commands == null) throw new ArgumentNullException("command");
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            this.sender = sender;
            this.documentStore = documentStore;
            this.command = commands;
            this.eventStore = eventStore;
        }
        public void Build()
        {
            command.WireToWhen(new CustomerApplicationService(eventStore));
            //DomainBoundedContext.Ports(sender).ForEach(events.WireToWhen);
            //DomainBoundedContext.Tasks(sender, viewDocs, true).ForEach(builder.AddTask);
            //DomainBoundedContext.FuncApplicationServices().ForEach(funcs.WireToWhen);
            //DomainBoundedContext.EntityApplicationServices(viewDocs, store,vector).ForEach(commands.WireToWhen);
            command.WireToWhen( new CustomerIndexService(documentStore.GetReader<unit, CustomerIndexLookUp>()));
            
        }

        public IEnumerable<object> Projections(IDocumentStore docs)
        {
            yield return new CustomerIndexProjection(docs.GetWriter<unit, CustomerIndexLookUp>());
        }

       
    }
}
