using System.Text.RegularExpressions;

namespace MailerCommon;

public class EmailSenderProxy : IEmailSender
{
    private readonly List<IEmailSender> _senders;

    public EmailSenderProxy(List<IEmailSender> senders)
    {
        _senders = senders;
    }

    public bool SendByDefault { get; set; } = true;

    public EmailSenderResult Send(EmailMessage message)
    {
        string emailPattern = @"^\S+@\S+$";
        if(!Regex.IsMatch(message.ToAddress, emailPattern))
        {
            return new EmailSenderResult { Status = "Invalid Email Address", EmailWasSent = false };
        }
        
        foreach (IEmailSender sender in _senders)
        {
            if(sender.IsSender(message))
            {
                return sender.Send(message);
            }
        }

        return new EmailSenderResult { Status = "No Sender Selected", EmailWasSent = false };
    }
}
