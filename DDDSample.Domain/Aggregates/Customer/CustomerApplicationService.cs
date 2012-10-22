using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample
{
    public class CustomerApplicationService : IApplicationService
    {
        readonly IEventStore _eventStore;
        public CustomerApplicationService(IEventStore eventStore)
        {
            if (eventStore == null) throw new ArgumentNullException("eventStore");
            _eventStore = eventStore;
        }
         public void Execute(ICommand command)
        {
            ((dynamic)this).When((dynamic)command);
        }
        public void When(CreateCustomer command)
        {
            Update(command.Id,a => a.Create(command.Id,command.CustomerName));
        }
        public void Update(CustomerId customerId, Action<CustomerAggregate> execute)
        {
            while (true)
            {
                var eventStream = _eventStore.LoadEventStream(id: customerId);
                var customer = new CustomerAggregate(eventStream.Events);
                execute(customer);
                try
                {
                     _eventStore.AppendEventsToStream(customerId, eventStream.StreamVersion, customer.Changes);
                    return;
                }
                catch (OptimisticConcurrencyException ex)
                {
                    foreach (var customerEvent in customer.Changes)
                    {
                        foreach (var actualEvent in ex.ActualEvents)
                        {
                            if (ConflictsWith(customerEvent,actualEvent))
                            {
                                var msg = string.Format("Conflict between {0} and {1}",customerEvent,actualEvent);
                                throw new RealConcurrencyException(msg,ex);
                            }
                        }
                    }
                }
            }

        }
        static bool ConflictsWith(IEvent x, IEvent y)
        {
            return x.GetType() == y.GetType();
        }

            
       
    }
}
