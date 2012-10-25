
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DDDSample
{
    public interface IProjection { }
    public interface IDomainService { }

    public interface ISampleMessage { }

    public interface ICommand : ISampleMessage { }

    public interface IEvent : ISampleMessage { }

    public interface ICommand<out TIdentity> : ICommand
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IApplicationService
    {
        void Execute(ICommand command);
    }


    public interface IEvent<out TIdentity> : IEvent
        where TIdentity : IIdentity
    {
        TIdentity Id { get; }
    }

    public interface IFuncCommand : ICommand { }

    public interface IFuncEvent : IEvent { }

    /// <summary>The only messaging endpoint that is available to stateless services
    /// They are not allowed to send any other messages.</summary>
    public interface IEventPublisher
    {
        void Publish(IFuncEvent notification);
        void PublishBatch(IEnumerable<IFuncEvent> events);
    }


    public interface IAggregate<in TIdentity>
        where TIdentity : IIdentity
    {
        void Execute(ICommand<TIdentity> c);
    }


    public interface IAggregateState
    {
        void Apply(IEvent<IIdentity> e);
    }


    /// <summary>
    /// Semi strongly-typed message sending endpoint made
    ///  available to stateless workflow processes.
    /// </summary>
    public interface ICommandSender
    {
        /// <summary>
        /// This interface is intentionally made long and unusable. Generally within the domain 
        /// (as in Mousquetaires domain) there will be extension methods that provide strongly-typed
        /// extensions (that don't allow sending wrong command to wrong location).
        /// </summary>
        /// <param name="commands">The commands.</param>
        void SendCommand(ICommand commands, bool idFromContent = false);
    }


    public interface IEventStore
    {
        EventStream LoadEventStream(IIdentity id);
        void AppendEventsToStream(IIdentity id, long version, ICollection<IEvent> events);
    }

    /// <summary>
    /// Is thrown by event store if there were changes since our last version
    /// </summary>
    [Serializable]
    public class OptimisticConcurrencyException : Exception
    {
        public long ActualVersion { get; private set; }
        public long ExpectedVersion { get; private set; }
        public IIdentity Id { get; private set; }
        public IList<IEvent> ActualEvents { get; private set; }

        OptimisticConcurrencyException(string message, long actualVersion, long expectedVersion, IIdentity id,
            IList<IEvent> serverEvents)
            : base(message)
        {
            ActualVersion = actualVersion;
            ExpectedVersion = expectedVersion;
            Id = id;
            ActualEvents = serverEvents;
        }

        public static OptimisticConcurrencyException Create(long actual, long expected, IIdentity id,
            IList<IEvent> serverEvents)
        {
            var message = string.Format("Expected v{0} but found v{1} in stream '{2}'", expected, actual, id);
            return new OptimisticConcurrencyException(message, actual, expected, id, serverEvents);
        }

        protected OptimisticConcurrencyException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }

    public sealed class EventRecord
    {
        public long StreamVersion;
        public ICollection<IEvent> Events;
    }

    public sealed class EventStream
    {
        public long StreamVersion;
        public List<IEvent> Events = new List<IEvent>();
    }
    [Serializable]
    public class DomainError : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public DomainError() { }
        public DomainError(string message) : base(message) { }
        public DomainError(string format, params object[] args) : base(string.Format(format, args)) { }

        /// <summary>
        /// Creates domain error exception with a string name, that is easily identifiable in the tests
        /// </summary>
        /// <param name="name">The name to be used to identify this exception in tests.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public static DomainError Named(string name, string format, params object[] args)
        {
            var message = "[" + name + "] " + string.Format(format, args);
            return new DomainError(message)
            {
                Name = name
            };
        }

        public string Name { get; private set; }

        public DomainError(string message, Exception inner) : base(message, inner) { }

        protected DomainError(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
    [Serializable]
    public class RealConcurrencyException : Exception
    {
        public RealConcurrencyException() { }
        public RealConcurrencyException(string message) : base(message) {}
        public RealConcurrencyException(string message, Exception inner) : base(message,inner) {}
        public RealConcurrencyException(SerializationInfo info, StreamingContext context) : base(info,context) {}


    }
}