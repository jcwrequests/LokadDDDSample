using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DDDSample
{
    [DataContract(Namespace = "DDDSample")]
    public sealed class CreateCustomer : ICommand<CustomerId>
    {
        public CreateCustomer(CustomerId customerId, string customerName)
        {
            this.Id = customerId;
            this.CustomerName = customerName;
        }

        [DataMember(Order = 1)]
        public CustomerId Id { get; private set; }
        [DataMember(Order = 2)]
        public string CustomerName { get; private set; }

    }
    [DataContract(Namespace = "DDDSample")]
    public sealed class CustomerCreated : IEvent<CustomerId>
    {
        public CustomerCreated(CustomerId customerId, string customerName)
        {
            this.Id = customerId;
            this.CustomerName = customerName;
        }

        [DataMember(Order = 1)]
        public CustomerId Id { get; private set; }
        [DataMember(Order = 2)]
        public string CustomerName { get; private set; }

    }

    [DataContract(Namespace = "DDDSample")]
    public partial class InstanceStarted : IFuncEvent
    {
        [DataMember(Order = 1)]
        public string CodeVersion { get; private set; }
        [DataMember(Order = 2)]
        public string Role { get; private set; }
        [DataMember(Order = 3)]
        public string Instance { get; private set; }

        InstanceStarted() { }
        public InstanceStarted(string codeVersion, string role, string instance)
        {
            CodeVersion = codeVersion;
            Role = role;
            Instance = instance;
        }

        public override string ToString()
        {
            return string.Format(@"Started {0}. {1}", Role, Instance);
        }
    }
    [DataContract(Namespace = "DDDSample")]
    public partial class SendMailMessage : IFuncCommand
    {
        [DataMember(Order = 1)]
        public Email[] To { get; private set; }
        [DataMember(Order = 2)]
        public string Subject { get; private set; }
        [DataMember(Order = 3)]
        public string Body { get; private set; }
        [DataMember(Order = 4)]
        public bool IsHtml { get; private set; }
        [DataMember(Order = 5)]
        public Email[] Cc { get; private set; }
        [DataMember(Order = 6)]
        public Email OptionalSender { get; private set; }
        [DataMember(Order = 7)]
        public Email OptionalReplyTo { get; private set; }

        SendMailMessage()
        {
            To = new Email[0];
            Cc = new Email[0];
        }
        public SendMailMessage(Email[] to, string subject, string body, bool isHtml, Email[] cc, Email optionalSender, Email optionalReplyTo)
        {
            To = to;
            Subject = subject;
            Body = body;
            IsHtml = isHtml;
            Cc = cc;
            OptionalSender = optionalSender;
            OptionalReplyTo = optionalReplyTo;
        }
    }
    [DataContract(Namespace = "DDDSample")]
    public partial class MailMessageSent : IFuncEvent
    {
        [DataMember(Order = 1)]
        public Email[] To { get; private set; }
        [DataMember(Order = 2)]
        public string Subject { get; private set; }
        [DataMember(Order = 3)]
        public string Body { get; private set; }
        [DataMember(Order = 4)]
        public bool IsHtml { get; private set; }
        [DataMember(Order = 5)]
        public Email[] Cc { get; private set; }
        [DataMember(Order = 6)]
        public Email OptionalSender { get; private set; }
        [DataMember(Order = 7)]
        public Email OptionalReplyTo { get; private set; }

        MailMessageSent()
        {
            To = new Email[0];
            Cc = new Email[0];
        }
        public MailMessageSent(Email[] to, string subject, string body, bool isHtml, Email[] cc, Email optionalSender, Email optionalReplyTo)
        {
            To = to;
            Subject = subject;
            Body = body;
            IsHtml = isHtml;
            Cc = cc;
            OptionalSender = optionalSender;
            OptionalReplyTo = optionalReplyTo;
        }
    }
    [DataContract(Namespace = "DDDSample")]
    public partial class EventStreamStarted : IFuncEvent
    {
    }
    [DataContract(Namespace = "DDDSample")]
    public partial class MessageQuarantined : IFuncEvent
    {
        [DataMember(Order = 1)]
        public string Log { get; private set; }
        [DataMember(Order = 2)]
        public byte[] Envelope { get; private set; }
        [DataMember(Order = 3)]
        public string[] Contracts { get; private set; }
        [DataMember(Order = 4)]
        public DateTime TimeUtc { get; private set; }

        MessageQuarantined()
        {
            Envelope = new byte[0];
            Contracts = new string[0];
        }
        public MessageQuarantined(string log, byte[] envelope, string[] contracts, DateTime timeUtc)
        {
            Log = log;
            Envelope = envelope;
            Contracts = contracts;
            TimeUtc = timeUtc;
        }
    }
}