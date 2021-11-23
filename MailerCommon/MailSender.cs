//using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.ComponentModel;

namespace Mailer.Sender;
public class Simple
{
    static bool mailSent = false;
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
        mailSent = true;
    }

    public static void Send(Message msg)
    {
        // I copied this from here: https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.sendasync?view=net-6.0#System_Net_Mail_SmtpClient_SendAsync_System_String_System_String_System_String_System_String_System_Object_

        // if(!msg.Text.Contains("This is a test."))
        // {
        //     return;
        // }

        // Command-line argument must be the SMTP host.
        string smtpHost = "smtp"; // A Linked Docker Container
        SmtpClient client = new SmtpClient(smtpHost);
        // Specify the email sender.
        // Create a mailing address that includes a UTF8 character
        // in the display name.
        MailAddress from = new MailAddress("some@email.com",
            "My City My Group Services",
        System.Text.Encoding.UTF8);
        // Set destinations for the email message.
        MailAddress to = new MailAddress(msg.ToAddress);
        // Specify the message content.
        MailMessage message = new MailMessage(from, to);
        message.Body = msg.Text;
        // Include some non-ASCII characters in body and subject.
        message.BodyEncoding =  System.Text.Encoding.UTF8;
        message.Subject = msg.Subject;
        message.SubjectEncoding = System.Text.Encoding.UTF8;
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
        Console.WriteLine("Goodbye.");
    }

    public static void Run()
    {
        // I copied this from here: https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient.sendasync?view=net-6.0#System_Net_Mail_SmtpClient_SendAsync_System_String_System_String_System_String_System_String_System_Object_

        // Command-line argument must be the SMTP host.
        string smtpHost = "smtp"; // A Linked Docker Container
        SmtpClient client = new SmtpClient(smtpHost);
        // Specify the email sender.
        // Create a mailing address that includes a UTF8 character
        // in the display name.
        MailAddress from = new MailAddress("some@email.com",
            "Territory " + (char)0xD8+ " Tools System",
        System.Text.Encoding.UTF8);
        // Set destinations for the email message.
        MailAddress to = new MailAddress("some@email.com");
        // Specify the message content.
        MailMessage message = new MailMessage(from, to);
        message.Body = "This is a test email message sent by an application. ";
        // Include some non-ASCII characters in body and subject.
        string someArrows = new string(new char[] {'\u2190', '\u2191', '\u2192', '\u2193'});
        message.Body += Environment.NewLine + someArrows;
        message.BodyEncoding =  System.Text.Encoding.UTF8;
        message.Subject = "test message 1" + someArrows;
        message.SubjectEncoding = System.Text.Encoding.UTF8;
        // Set the method that is called back when the send operation ends.
        client.SendCompleted += new
            SendCompletedEventHandler(SendCompletedCallback);
        // The userState can be any object that allows your callback
        // method to identify this send operation.
        // For this example, the userToken is a string constant.
        string userState = "test message1";
        client.SendAsync(message, userState);
        Console.WriteLine("Sending message... press c to cancel mail. Press any other key to exit.");
        string answer = Console.ReadLine();
        // If the user canceled the send, and mail hasn't been sent yet,
        // then cancel the pending operation.
        if (answer.StartsWith("c") && mailSent == false)
        {
            client.SendAsyncCancel();
        }
        // Clean up.
        message.Dispose();
        Console.WriteLine("Goodbye.");
    }
}