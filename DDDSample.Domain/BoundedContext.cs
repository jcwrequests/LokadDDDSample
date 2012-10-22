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
            this.ApplicationServices();

        }

        private  IEnumerable<object> Ports()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Func<System.Threading.CancellationToken, System.Threading.Tasks.Task>> Tasks()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> Projections()
        {
            throw new NotImplementedException();
        }

        private IEnumerable<IApplicationService> ApplicationServices()
        {
            yield return new CustomerApplicationService(eventStore);
        }

        private IEnumerable<object> FuncApplicationServices()
        {
            yield break;
        }
    }
}
