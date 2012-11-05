using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample.EndPoint.Query
{
    public sealed class QueryService
    {
        private CustomerIndexService _index;

        public QueryService(IDocumentStore store)
        {
            if (store == null) throw new ArgumentNullException("store");
            _index = new CustomerIndexService(store.GetReader<unit, CustomerIndexLookUp>());
        }

        public CustomerId GetId(string customerName)
        {
            return _index.GetCustomerId(customerName);
        }
    }
}
