using Mailer.Sender;
using MailerCommon.Configuration;
using SendGrid;

namespace MailerCommon
{
    public class SendGridEmailSender : IEmailSender
    {
        readonly string _sendGridApiKey;
        readonly ScheduleOptions _options;
        readonly SendGridEmailer _emailer;

        public SendGridEmailSender(string sendGridApiKey, ScheduleOptions options)
        {
            _sendGridApiKey = sendGridApiKey;
            _options = options;
            _emailer = new SendGridEmailer(options);
        }

        public bool SendByDefault { get; set; } = true;

        public EmailSenderResult Send(EmailMessage message)
        {
            string status = $"{DateTime.Now}: Preparing SendMail Message";
            bool wasSent = false;

            try
            {
                Response response = _emailer.SendEmail(
                    message.ToName, 
                    message.ToAddress, 
                    _sendGridApiKey, 
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
