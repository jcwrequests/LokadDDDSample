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
                              RedirectToCommand command,
                              IEventStore eventStore)
        {
            if (sender == null) throw new ArgumentNullException("sender");
            if (documentStore == null) throw new ArgumentNullException("store");
            if (command == null) throw new ArgumentNullException("command");
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            this.sender = sender;
            this.documentStore = documentStore;
            this.command = command;
            this.eventStore = eventStore;
        }
        public void Build()
        {
            new CustomerApplicationService(eventStore);
            //DomainBoundedContext.Ports(sender).ForEach(events.WireToWhen);
            //DomainBoundedContext.Tasks(sender, viewDocs, true).ForEach(builder.AddTask);
            //DomainBoundedContext.FuncApplicationServices().ForEach(funcs.WireToWhen);
            //DomainBoundedContext.EntityApplicationServices(viewDocs, store,vector).ForEach(commands.WireToWhen);
        }

        public IEnumerable<object> Projections()
        {
            yield break;
        }

       
    }
}
