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
            string status = $"{DateTime.Now}: Preparing SendMail Message";
            bool wasSent = false;

            try
            {
                Response response = SendGridEmailer.SendEmail(
                    message.ToName, 
                    message.ToAddress, 
                    sendGridApiKey, 
                    message.Subject, 
                    message.Text).Result;

                status = $"{DateTime.Now}: SendMail Status Code:{response.StatusCode}";
                wasSent = response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                status = $"{DateTime.Now}: SendMail Error: {ex.Message}";
                wasSent = false;
            }
            
            return new EmailSenderResult { Status = status, EmailWasSent = wasSent };
        }
    }
}
