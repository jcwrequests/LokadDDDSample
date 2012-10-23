using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using Lokad.Cqrs;

namespace DDDSample
{
    public class CustomerIndexProjection
    {
        IDocumentWriter<unit, CustomerIndexLookUp> writer;

        public CustomerIndexProjection(IDocumentWriter<unit, CustomerIndexLookUp> writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            this.writer = writer;
        }
        public void When(CustomerCreated e)
        {
            Func<CustomerIndexLookUp> create;
            create = ()=> {
                            var index = new CustomerIndexLookUp();
                            index.Customers.Add(key: e.CustomerName, value: e.Id);
                            return index;
                          };
            writer.AddOrUpdate(unit.it, 
                               create(), 
                               i => i.Customers.Add(key: e.CustomerName, 
                                                    value: e.Id));

        }
    }
}
