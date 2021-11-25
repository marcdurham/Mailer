namespace MailerCommon
{
    public interface IEmailSender
    {
        EmailSenderResult Send(EmailMessage message);
    }
}
