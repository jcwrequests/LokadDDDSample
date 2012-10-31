using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDDSample.Engine
{
    public class TestDispatcher : IDispatcher
    {
        void IDispatcher.Dispatch(IEvent @event)
        {
            Console.WriteLine(@event.ToString());
        }

        void IDispatcher.Dispatch(IEnumerable<IEvent> events)
        {
            foreach (var e in events)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
