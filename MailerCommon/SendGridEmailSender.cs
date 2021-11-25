using Mailer.Sender;
using SendGrid;

namespace MailerCommon
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly string sendGridApiKey;

        public SendGridEmailSender(string sendGridApiKey)
        {
            this.sendGridApiKey = sendGridApiKey;
        }

        public bool SendByDefault { get; set; } = true;

        public EmailSenderResult Send(EmailMessage message)
        {
            string status = "Preparing SendMail Message";

            try
            {
                Response response = SendGridEmailer.SendEmail(
                    message.ToName, 
                    message.ToAddress, 
                    sendGridApiKey, 
                    message.Subject, 
                    message.Text).Result;

                status = $"SendMail Status Code:{response.StatusCode}";
            }
            catch (Exception ex)
            {
                status = $"SendMail Error: {ex.Message}";
            }
            
            return new EmailSenderResult { Status = status };
        }
    }
}
