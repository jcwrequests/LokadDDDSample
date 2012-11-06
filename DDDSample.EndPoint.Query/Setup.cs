using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample.EndPoint.Query
{
    public sealed class Setup
    {
        private Func<IDocumentStrategy, IDocumentStore> CreateDocs;
        private static readonly IDocumentStrategy ViewStrategy = new ViewStrategy();

        public Setup ConfigDocumentStore(Func<IDocumentStrategy, IDocumentStore> createDocs)
        {
            this.CreateDocs = createDocs;
            return this;
        }

        public Container Build()
        {
            var viewDocs = CreateDocs(ViewStrategy);

            return new Container
            {
                Setup = this,
                ViewDocs = viewDocs
            };
        }
    }
}
