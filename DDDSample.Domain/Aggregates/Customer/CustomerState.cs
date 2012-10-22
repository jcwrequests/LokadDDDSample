using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample
{
    public class CustomerState
    {
        private CustomerId _id; 
        private string _name;
        private bool _created;

        public CustomerState(IEnumerable<IEvent> events)
        {
            foreach (var e in events)
            {
                Mutate(e);
            }
        }
        public bool Created {get {return _created;}} 
        

        public void Mutate(IEvent e)
        {
            ((dynamic) this).When((dynamic)e);
        }
        
        public  void When(CustomerCreated e)
        {
            this.Id = e.Id;
            this.Name = e.CustomerName;
            this._created = true;
        }
       
        public CustomerId Id{get;private set;}
        public string Name {get;private set;}
    }
}
