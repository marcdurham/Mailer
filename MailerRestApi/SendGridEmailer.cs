using SendGrid;
using SendGrid.Helpers.Mail;

namespace  Mailer.Sender;

public class SendGridEmailer
{
    public static async Task<Response> SendEmail(string name, string email, string sendGridApiKey)
    {
        var client = new SendGridClient(sendGridApiKey);
        var from = new EmailAddress("some@email.com", "Information Mailer System");
        var subject = "My Group Schedule";
        var to = new EmailAddress(email, name);
        var plainTextContent = "and easy to do anywhere, even with C#";
        var htmlContent = "<strong>and easy to do anywhere, even with C#</strong>";
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
        var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        return response;
    }
}
