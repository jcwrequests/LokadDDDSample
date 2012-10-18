using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Cqrs;
using Lokad.Cqrs.TapeStorage;
using DDDSample.Contracts;


public sealed class EventStore : IEventStore
{
    readonly MessageStore _store;

    public EventStore(MessageStore store)
    {
        _store = store;
    }

    public void AppendEventsToStream(IIdentity id, long originalVersion, ICollection<IEvent> events)
    {
        if (events.Count == 0) return;
        // functional events don't have an identity
        var name = IdentityToKey(id);

        try
        {
            _store.AppendToStore(name, MessageAttribute.Empty, originalVersion, events.Cast<object>().ToArray());
        }
        catch (AppendOnlyStoreConcurrencyException e)
        {
            // load server events
            var server = LoadEventStream(id);
            // throw a real problem
            throw OptimisticConcurrencyException.Create(server.Version, e.ExpectedStreamVersion, id, server.Events);
        }
    }

    static string IdentityToKey(IIdentity id)
    {
        return id == null ? "func" : (id.GetTag() + ":" + id.GetId());
    }

    public EventStream LoadEventStream(IIdentity id)
    {
        var key = IdentityToKey(id);

        // TODO: make this lazy somehow?
        var stream = new EventStream();
        foreach (var record in _store.EnumerateMessages(key, 0, int.MaxValue))
        {
            stream.Events.AddRange(record.Items.Cast<IEvent>());
            stream.Version = record.StreamVersion;
        }
        return stream;
    }
}
