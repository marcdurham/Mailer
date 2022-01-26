using MailerCommon.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace  Mailer.Sender;

public class SendGridEmailer
{
    private readonly ScheduleOptions _options;

    public SendGridEmailer(ScheduleOptions options)
    {
        _options = options;
    }

    public async Task<Response> SendEmail(string name, string email, string sendGridApiKey, string subject, string htmlContent)
    {
        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress(
            _options.EmailFromAddress ?? "auto@mailer.org", 
            _options.EmailFromName ?? "Mailer Information Board");
       
        var to = new EmailAddress(email, name);
        var plainTextContent = subject;
        
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        return response;
    }
}
