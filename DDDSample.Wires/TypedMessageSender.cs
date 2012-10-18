using Lokad.Cqrs;
using DDDSample;


    public sealed class TypedMessageSender : ICommandSender
    {
        readonly MessageSender _commandRouter;
        readonly MessageSender _functionalRecorder;


        public TypedMessageSender(MessageSender commandRouter, MessageSender functionalRecorder)
        {
            _commandRouter = commandRouter;
            _functionalRecorder = functionalRecorder;
        }

        public void SendCommand(ICommand commands, bool idFromContent = false)
        {
            if (idFromContent)
            {
                _commandRouter.SendHashed(commands);
            }
            else
            {
                _commandRouter.Send(commands);
            }
        }

        public void SendFromClient(ICommand e, string id, MessageAttribute[] attributes)
        {
            _commandRouter.Send(e, id, attributes);
        }

        public void PublishFromClientHashed(IFuncEvent e, MessageAttribute[] attributes)
        {
            _functionalRecorder.SendHashed(e, attributes);
        }

        public void Publish(IFuncEvent @event)
        {
            _functionalRecorder.Send(@event);
        }

        public void Send(IFuncCommand command)
        {
            _commandRouter.Send(command);
        }

        public void SendHashed(IFuncCommand command)
        {
            _commandRouter.SendHashed(command);
        }


    }
