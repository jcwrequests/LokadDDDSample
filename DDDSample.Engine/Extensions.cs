using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.AtomicStorage;
using DDDSample.Engine;
using DDDSample;
using Lokad.Cqrs.Build;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Partition;
using Lokad.Cqrs.StreamingStorage;
using Lokad.Cqrs.TapeStorage;



    public static class Extensions
    {
        public static MessageSender CreateQueue(this EnvelopeStreamer streamer, Func<string, IQueueWriter> createQueueWriter, string queueName)
        {

            return new MessageSender(streamer, createQueueWriter(queueName));

        }
    }

