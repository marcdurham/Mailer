namespace MailerCommon
{
    public class SaveEmailToFileEmailSender : IEmailSender
    {
        public bool SendByDefault { get; set; } = false;

        public EmailSenderResult Send(EmailMessage message)
        {
            File.WriteAllText(
                path: $"{message.ToName};{message.ToAddress};{message.Subject.Replace(":", "")}.html", 
                contents: message.Text);

            return new EmailSenderResult 
            { 
                Status = "Saved to file", 
                EmailWasSent = true 
            };
        }
    }
}
