namespace MailerCommon
{
    internal class FakeFileEmailSender : IEmailSender
    {
        public EmailSenderResult Send(EmailMessage message)
        {
            File.WriteAllText($"{message.ToName}.{message.ToAddress}.{message.Subject.Replace(":", "")}.html", message.Text);

            return new EmailSenderResult { Status = "Saved to file" };
        }
    }
}
