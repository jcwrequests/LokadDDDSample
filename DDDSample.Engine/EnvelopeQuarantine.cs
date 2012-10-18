using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Lokad.Cqrs;
using Lokad.Cqrs.Envelope;
using Lokad.Cqrs.Evil;
using Lokad.Cqrs.StreamingStorage;
using ServiceStack.Text;
using DDDSample;


public sealed class EnvelopeQuarantine : IEnvelopeQuarantine
{
    readonly IStreamContainer _root;
    readonly IEnvelopeStreamer _streamer;

    readonly MemoryQuarantine _quarantine = new MemoryQuarantine();
    readonly TypedMessageSender _writer;

    public EnvelopeQuarantine(IEnvelopeStreamer streamer,
        TypedMessageSender writer, IStreamContainer root)
    {
        _streamer = streamer;
        _writer = writer;
        _root = root;
        _root.Create();
    }

    public bool TryToQuarantine(ImmutableEnvelope context, Exception ex)
    {
        // quit immediately, we don't want an endless cycle!
        //if (context.Items.Any(m => m.Content is MessageQuarantined))
        //    return true;
        var quarantined = _quarantine.TryToQuarantine(context, ex);

        try
        {
            var file = string.Format("{0:yyyy-MM-dd}-{1}-engine.txt",
                DateTime.UtcNow,
                context.EnvelopeId.ToLowerInvariant());



            var data = "";
            if (_root.Exists(file))
            {
                using (var stream = _root.OpenRead(file))
                using (var reader = new StreamReader(stream))
                {
                    data = reader.ReadToEnd();
                }
            }

            var builder = new StringBuilder(data);
            if (builder.Length == 0)
            {
                DescribeMessage(builder, context);
            }

            builder.AppendLine("[Exception]");
            builder.AppendLine(DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine(ex.ToString());
            builder.AppendLine();

            var text = builder.ToString();

            using (var stream = _root.OpenWrite(file))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(text);
            }

            if (quarantined)
            {
                ReportFailure(text, context);
            }
        }
        catch (Exception x)
        {
            SystemObserver.Notify(x.ToString());
        }

        return quarantined;
    }

    public void Quarantine(byte[] message, Exception ex) { }


    public void TryRelease(ImmutableEnvelope context)
    {
        _quarantine.TryRelease(context);
    }

    static void DescribeMessage(StringBuilder builder, ImmutableEnvelope context)
    {
        builder.AppendLine(context.PrintToString(o => JsvFormatter.Format(JsonSerializer.SerializeToString(o))));
    }

    void ReportFailure(string text, ImmutableEnvelope envelope)
    {
        // we do not report failure on attempts to send mail messages
        // this could be a loop
        // we can't report them anyway (since mail sending is not available)
        // note, that this could be an indirect loop.
        if (text.Contains(typeof(SendMailMessage).Name))
            return;

        if (text.Contains(typeof(MessageQuarantined).Name))
            return;

        var name = envelope.Message.GetType().Name.Replace("Command", "");

        var subject = string.Format("[Error]: S2 fails '{0}'", name);


        var builder = new StringBuilder();
        builder.AppendFormat(
            @"<p>Support,</p><p>Something just went horribly wrong - there is a problem that I can't resolve. Please check the error log <strong>ASAP</strong>.</p>
                    <p>Here are a few details to help you out:</p><pre>");

        builder.AppendLine(WebUtility.HtmlEncode(text));

        builder.AppendFormat("</pre><p>You can use S2 Maintenance to get the error details.</p><p>Sincerely,<br /> Hub AI</p>");




        // if we don't fit in the limits
        if (text.Length >= 1024 * 1024)
        {
            const string body = "Subj, please notify Lokad support team immediately";
            const string subj = "[S2]: Quarantine overflow";
            var message = new SendMailMessage(new[] { new Email("contact@lokad.com") }, subj,
                body, false, null, null, null);
            _writer.SendCommand(message, true);
            
            // fail immediately

            return;
        }

        var to = new[]
            {
                new Email("contact@lokad.com", "Salescast Support"),
                new Email("rinat.abdullin@gmail.com", "Rinat Abdullin")
            };
        var cmd = new SendMailMessage(to, subject, builder.ToString(), true, null, null, null);
        _writer.SendCommand(cmd, true);
        var buffer = _streamer.SaveEnvelopeData(envelope);
        var names = new[] { ContractEvil.GetContractReference(envelope.Message.GetType()) };
        _writer.Publish(new MessageQuarantined(text, buffer, names, DateTime.UtcNow));
    }
}
