using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DDDSample
{
    [DataContract]
    public class CustomerIndexLookUp
    {
        [DataMember(Order = 1)]
        public IDictionary<string, CustomerId> Customers { get; private set; }

        public CustomerIndexLookUp()
        {
            Customers = new Dictionary<string, CustomerId>();    
        }
        
    }
}
