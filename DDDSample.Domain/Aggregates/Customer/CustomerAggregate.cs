using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample
{
     public class CustomerAggregate
    {
         public readonly IList<IEvent> Changes = new List<IEvent>();
         readonly CustomerState _state ;

         public CustomerAggregate(IEnumerable<IEvent> events)
         {
             _state = new CustomerState(events);
         }
        public void Create(CustomerId id, string name)
        {
             if (_state.Created) throw new InvalidOperationException("Customer Already Created");
             Apply(new CustomerCreated(id,name));
        }
       
        public void Apply(IEvent e)
        {
            _state.Mutate(e);
            Changes.Add(e);
        }

    }
}
