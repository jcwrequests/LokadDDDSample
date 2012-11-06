using DDDSample.EndPoint.Query.Properties;
using Lokad.Cqrs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample.EndPoint.Query
{
    class Program
    {
        static void Main(string[] args)
        {
            Setup setup = new Setup();

            var integrationPath = Settings.Default.storage;
            var path = integrationPath.Remove(0, 5);
            var config = FileStorage.CreateConfig(path);

            Container container = setup.
                                    ConfigDocumentStore(config.CreateDocumentStore).
                                    Build();

            QueryService service = new QueryService(container.ViewDocs);
            CustomerId id = service.GetId("Rinat Abdullin");
            Console.WriteLine(id);
            Console.ReadLine();

        }
    }
}
