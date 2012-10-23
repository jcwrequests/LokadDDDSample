using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.Evil;
using DDDSample;
using ServiceStack.Text;
using System.Threading;
using System.Diagnostics;
using Lokad.Cqrs;
using DDDSample.Engine.Properties;


namespace DDDSample.Engine
{
    class Program
    {
        static void Main()
        {
            using (var env = BuildEnvironment())
            using (var cts = new CancellationTokenSource())
            {
                env.ExecuteStartupTasks(cts.Token);
                using (var engine = env.BuildEngine(cts.Token))
                {
                    var task = engine.Start(cts.Token);

                    env.SendToCommandRouter.Send(new CreateCustomer(new CustomerId(1),"Rinat Abdullin"));

                    Console.WriteLine(@"Press enter to stop");
                    Console.ReadLine();
                    cts.Cancel();
                    if (!task.Wait(5000))
                    {
                        Console.WriteLine(@"Terminating");
                    }
                }
            }
        }


        static void ConfigureObserver()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var observer = new ConsoleObserver();
            SystemObserver.Swap(observer);
            Context.SwapForDebug(s => SystemObserver.Notify(s));
        }

        public static Container BuildEnvironment()
        {
            //JsConfig.DateHandler = JsonDateHandler.ISO8601;
            ConfigureObserver();
            var integrationPath =  Settings.Default.storage; //AzureSettingsProvider.GetStringOrThrow(Conventions.StorageConfigName);
            //var email = AzureSettingsProvider.GetStringOrThrow(Conventions.SmtpConfigName);
            

            //var core = new SmtpHandlerCore(email);
            var setup = new Setup
            {
                //Smtp = core,
                //FreeApiKey = freeApiKey,
                //WebClientUrl = clientUri,
                //HttpEndpoint = endPoint,
                //EncryptorTool = new EncryptorTool(systemKey)
            };

            if (integrationPath.StartsWith("file:"))
            {
                var path = integrationPath.Remove(0, 5);

                SystemObserver.Notify("Using store : {0}", path);

                var config = FileStorage.CreateConfig(path);

                return setup.
                    ConfigStreaming(config.CreateStreaming()).
                    ConfigCreateDoc(config.CreateDocumentStore).
                    ConfigCreateInbox(s => config.CreateInbox(s, DecayEvil.BuildExponentialDecay(500))).
                    ConfigCreateTapes(config.CreateAppendOnlyStore).
                    ConfigCreateQueueWriter(config.CreateQueueWriter).
                    ConfigureQueues(1,1).
                    Build();

                
            }
            //if (integrationPath.StartsWith("Default") || integrationPath.Equals("UseDevelopmentStorage=true", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    var config = AzureStorage.CreateConfig(integrationPath);
            //    setup.Streaming = config.CreateStreaming();
            //    setup.CreateDocs = config.CreateDocumentStore;
            //    setup.CreateInbox = s => config.CreateInbox(s);
            //    setup.CreateQueueWriter = config.CreateQueueWriter;
            //    setup.CreateTapes = config.CreateAppendOnlyStore;
            //    setup.ConfigureQueues(4, 4);
            //    return setup.Build();
            //}
            throw new InvalidOperationException("Unsupported environment");
        }
    }
   }

