using SendGrid;
using SendGrid.Helpers.Mail;

namespace  Mailer.Sender;

public class SendGridEmailer
{
    public static async Task<Response> SendEmail(string name, string email, string sendGridApiKey, string subject, string htmlContent)
    {
        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress("auto@mailer.org", "Mailer Information Board");
       
        var to = new EmailAddress(email, name);
        var plainTextContent = subject;
        
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        return response;
    }
}
