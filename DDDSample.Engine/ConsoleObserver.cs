using System;
using System.Diagnostics;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope.Events;
using ServiceStack.Text;

namespace DDDSample
{
    public sealed class ConsoleObserver : IObserver<ISystemEvent>
    {
        readonly Stopwatch _watch = Stopwatch.StartNew();

        public void OnNext(ISystemEvent value)
        {
            ((dynamic)this).When((dynamic)value);
        }

        void When(EnvelopeDeserializationFailed e)
        {
            WriteLine(e.ToString());
        }

        void When(EnvelopeDispatched ed)
        {
            if (ed.Dispatcher != "watch")
                return;

            var content = ed.Envelope.Message;
            var eEvent = content as IEvent<IIdentity>;
            if (eEvent != null)
            {
                WriteLine("D{" + eEvent.Id + "} " + Describe.Object(content), ConsoleColor.DarkGreen);
                return;
            }
            if (content is IFuncEvent)
            {
                WriteLine("=> " + Describe.Object(content), ConsoleColor.DarkGreen);
                return;
            }
            //var adaptCommand = content as IAdaptCommand<IIdentity>;
            //if (adaptCommand != null)
            //{
            //    WriteLine("A{" + adaptCommand.Id + "} " + Describe.Object(content), ConsoleColor.DarkCyan);
            //    return;
            //}
            if (content is IFuncCommand)
            {
                WriteLine("=> " + Describe.Object(content), ConsoleColor.DarkCyan);
                return;
            }

            if (content is ICommand)
            {
                WriteLine(Describe.Object(content), ConsoleColor.DarkCyan);
                return;
            }

            WriteLine(Describe.Object(content));
        }

        void When(SystemObserver.MessageEvent e)
        {
            WriteLine(e.Message);
        }

        void When(EnvelopeQuarantined e)
        {
            WriteLine("Envelope quarantined: " + e.LastException);
        }

        void When(object e)
        {
            // do nothing
            if (e is ISystemWarningEvent)
            {
                WriteLine(e.ToString(), ConsoleColor.DarkYellow);
            }
        }

        public void WriteLine(string line, ConsoleColor? passedColor = null)
        {

            var color = Console.ForegroundColor;
            var newCol = passedColor ?? color;
            if (line.StartsWithIgnoreCase("[warn"))
            {
                newCol = ConsoleColor.DarkYellow;
            }
            else if (line.StartsWithIgnoreCase("[good"))
            {
                newCol = ConsoleColor.DarkGreen;
            }
            else if (line.StartsWithIgnoreCase("[sys"))
            {
                newCol = ConsoleColor.DarkGray;
            }

            if (newCol == color)
            {
                var prefix = String.Format("[{0:0000000}]: ", _watch.ElapsedMilliseconds);
                Console.WriteLine(GetAdjusted(prefix, line));
            }
            else
            {
                Console.ForegroundColor = newCol;

                var prefix = String.Format("[{0:0000000}]: ", _watch.ElapsedMilliseconds);
                Console.WriteLine(GetAdjusted(prefix, line));
                Console.ForegroundColor = color;
            }
        }

        static string GetAdjusted(string adj, string text)
        {
            bool first = true;
            var builder = new StringBuilder();
            foreach (var s in text.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                builder.Append(first ? adj : new string(' ', adj.Length));
                builder.AppendLine(s);
                first = false;
            }
            return builder.ToString().TrimEnd();
        }


        public void OnError(Exception error) { }

        public void OnCompleted() { }
    }
}