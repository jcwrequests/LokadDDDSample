using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;

namespace DDDSample.Engine
{
    public class FluentCqrsEngineBuilder
    {
        CqrsEngineBuilder _builder;
        public FluentCqrsEngineBuilder(CqrsEngineBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException("bulder");
            this._builder = builder;
        }
        public FluentCqrsEngineBuilder(IEnvelopeStreamer streamer, IEnvelopeQuarantine quarantine = null, DuplicationManager duplication = null)
        {
            _builder = new CqrsEngineBuilder(streamer, quarantine, duplication);
        }
        public FluentCqrsEngineBuilder AddTask(IEngineProcess process)
        {
            _builder.AddTask(process);
            return this;
        }
        public FluentCqrsEngineBuilder Build(System.Threading.CancellationToken token)
        {
            _builder.Build(token);
            return this;
        }
        public FluentCqrsEngineBuilder Dispatch(Lokad.Cqrs.Partition.IPartitionInbox inbox, Action<byte[]> lambda)
        {
            _builder.Dispatch(inbox, lambda);
            return this;
        }
        public FluentCqrsEngineBuilder Handle(Lokad.Cqrs.Partition.IPartitionInbox inbox, Action<ImmutableEnvelope> lambda, string name = null)
        {
            _builder.Handle(inbox, lambda,name);
            return this;
        }
        //public FluentCqrsEngineBuilder Handle(string[] queues, Func<string, IPartitionInbox> inbox, Action<ImmutableEnvelope> lambda, string name = null)
        //{
        //    queues.ForEach(queue => _builder.Handle(inbox(queue), lambda,name));
        //    return this;
        //}
        public FluentCqrsEngineBuilder Handle( Func<string, IPartitionInbox> inbox, Action<ImmutableEnvelope> lambda, string name = null,params string[] queues)
        {
            queues.ForEach(queue => _builder.Handle(inbox(queue), lambda, name));
            return this;
        }
        public FluentCqrsEngineBuilder AddTask(IEngineProcess process)
        {
            _builder.AddTask(process);
            return this;
        }
        public FluentCqrsEngineBuilder AddTask(Func<System.Threading.CancellationToken,System.Threading.Tasks.Task> factoryToStartTask)
        {
            _builder.AddTask(factoryToStartTask);
            return this;
        }
        public CqrsEngineBuilder Instance {get{return _builder;}}
        
    }
}
