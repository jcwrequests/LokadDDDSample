using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs;

namespace DDDSample
{
    public class CustomerIndexService
    {
        IDocumentReader<unit, CustomerIndexLookUp> storage;
        public CustomerIndexService(IDocumentReader<unit, CustomerIndexLookUp> storage)
        {
            if (storage == null) throw new ArgumentNullException("storage");
            this.storage = storage;
        }
        public CustomerId GetCustomerId(string userName)
        {
            CustomerId customerId = null;
            storage.
                Get(unit.it).
                Value.
                Customers.
                TryGetValue(key: userName,
                            value: out customerId);
            return customerId;
        }
    }
}
