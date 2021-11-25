using MailerCommon;
using System.ComponentModel;
using System.Net.Mail;

namespace Mailer.Sender;
public class SmtpEmailSender : IEmailSender
{
    readonly Func<EmailMessage, bool> _isSender;
    public SmtpEmailSender(Func<EmailMessage, bool> isSender)
    {
        _isSender = isSender;
    }

    private static void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
    {
        // Get the unique identifier for this asynchronous operation.
        string token = (string) e.UserState;

        if (e.Cancelled)
        {
                Console.WriteLine("[{0}] Send canceled.", token);
        }
        if (e.Error != null)
        {
                Console.WriteLine("[{0}] {1}", token, e.Error.ToString());
        } else
        {
            Console.WriteLine("Message sent.");
        }
    }

    public bool IsSender(EmailMessage message)
    {
        return _isSender(message);
    }

    public EmailSenderResult Send(EmailMessage msg)
    {
        // I copied this from here: https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.sendasync?view=net-6.0#System_Net_Mail_SmtpClient_SendAsync_System_String_System_String_System_String_System_String_System_Object_
        string status = "Preparing to send SMTP email";

        string smtpHost = "smtp"; // smtp is the name of a linked service in the docker-compose.yml
        SmtpClient client = new(smtpHost);
        MailAddress from = new(msg.FromAddress, msg.FromName, System.Text.Encoding.UTF8);
        MailAddress to = new(msg.ToAddress);
        MailMessage message = new(from, to)
        {
            Body = msg.Text,
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            Subject = msg.Subject,
            SubjectEncoding = System.Text.Encoding.UTF8
        };

        // Set the method that is called back when the send operation ends.
        client.SendCompleted += new SendCompletedEventHandler(SendCompletedCallback);

        // The userState can be any object that allows your callback
        // method to identify this send operation.
        // For this example, the userToken is a string constant.
        string userState = "test message1";
        // //client.SendAsync(message, userState);
        client.Send(message);
        Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
        ////string answer = Console.ReadLine();
        // If the user canceled the send, and mail hasn't been sent yet,
        // then cancel the pending operation.
        ////if (answer.StartsWith("c") && mailSent == false)
        ////{
        ////    client.SendAsyncCancel();
        ////}
        // Clean up.
        ////////////////////////////////message.Dispose();
        
        return new EmailSenderResult { Status = "Done sending SMTP email" };
    }
}