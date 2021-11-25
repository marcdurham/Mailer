namespace MailerCommon
{
    public interface IEmailSender
    {
        EmailSenderResult Send(EmailMessage message);
        bool IsSender(EmailMessage message) => true;
    }
}
