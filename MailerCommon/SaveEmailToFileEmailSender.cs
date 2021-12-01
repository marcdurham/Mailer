namespace MailerCommon
{
    public class SaveEmailToFileEmailSender : IEmailSender
    {
        public bool SendByDefault { get; set; } = false;

        public EmailSenderResult Send(EmailMessage message)
        {
            File.WriteAllText($"{message.ToName}.{message.ToAddress}.{message.Subject.Replace(":", "")}.html", message.Text);

            return new EmailSenderResult { Status = "Saved to file", EmailWasSent = true };
        }
    }
}
