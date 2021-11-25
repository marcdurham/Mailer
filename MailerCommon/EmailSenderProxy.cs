namespace MailerCommon;

public class EmailSenderProxy : IEmailSender
{
    private readonly List<IEmailSender> _senders;

    public EmailSenderProxy(List<IEmailSender> senders)
    {
        _senders = senders;
    }

    public EmailSenderResult Send(EmailMessage message)
    {
        foreach(IEmailSender sender in _senders)
        {
            if(sender.IsSender(message))
            {
                return sender.Send(message);
            }
        }

        return new EmailSenderResult { Status = "Not Sent" };
    }
}
