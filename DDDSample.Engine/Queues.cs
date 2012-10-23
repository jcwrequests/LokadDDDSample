using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample
{
    public static class Queues
    {
        public static readonly string FunctionalRecorderQueue = Conventions.FunctionalEventRecorderQueue;
        public static readonly string RouterQueue = Conventions.DefaultRouterQueue;
        public const string EventProcessingQueue = Conventions.Prefix + "-handle-events";
        public const string AggregateHandlerQueue = Conventions.Prefix + "-handle-cmd-entity";
        
    }
    public static class Containers
    {
        public const string TapesContainer = Conventions.Prefix + "-tapes";
        public static readonly string ErrorsContainer = Conventions.DefaultErrorsFolder;
    }
}
