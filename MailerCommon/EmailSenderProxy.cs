namespace MailerCommon;

public class EmailSenderProxy : IEmailSender
{
    private readonly List<EmailSenderFunction> _senders;

    public EmailSenderProxy(List<EmailSenderFunction> senders)
    {
        _senders = senders;
    }

    public EmailSenderResult Send(EmailMessage message)
    {
        foreach(EmailSenderFunction sender in _senders)
        {
            if (sender.Function(message))
            {
                return sender.Sender.Send(message);
            }
        }

        return new EmailSenderResult { Status = "Not Sent" };
    }
}

public class EmailSenderFunction
{
    public EmailSenderFunction(IEmailSender sender, Func<EmailMessage, bool> function)
    {
        Sender = sender;
        Function = function;
    }

    public IEmailSender Sender { get; set; }
    public Func<EmailMessage, bool> Function { get; set; }
}
