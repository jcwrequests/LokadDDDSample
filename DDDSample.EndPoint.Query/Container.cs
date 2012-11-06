using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample.EndPoint.Query
{
    public sealed class Container
    {
        public Setup Setup;
        public IDocumentStore ViewDocs;
    }

}
