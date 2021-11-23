using GoogleAdapter.Adapters;
using SendGrid;
using System.Text.RegularExpressions;

namespace Mailer.Sender
{
public class PublisherEmailer 
{
    public static void Run()
    {

        Console.WriteLine("Reading and writing to a Google spreadsheet...");
//#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        // string json = Environment.GetEnvironmentVariable("ServiceSecretsJson", EnvironmentVariableTarget.Process)
        //     ?? throw new ArgumentNullException(nameof(json));
        string documentId = Environment.GetEnvironmentVariable("DocumentId", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(documentId));
        string range = Environment.GetEnvironmentVariable("Range", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(range));
        string sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(sendGridApiKey));
              
        string scheduleViewSheet = Environment.GetEnvironmentVariable("ScheduleViewSheet", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(scheduleViewSheet));
        string scheduleViewPublisherCell = Environment.GetEnvironmentVariable("ScheduleViewPublisherCell", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(scheduleViewPublisherCell));

//#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        string json = File.ReadAllText("/app/SendGrid.secrets.json");

        var sheets = new Sheets(json, isServiceAccount: true);

        IList<IList<object>> rows = sheets.Read(documentId: documentId, range: range);

        var publishers = new List<PublisherClass>();
        foreach (var r in rows)
        {
            string? sent = r.Count > 2 ? $"{r[2]}" : null;
            publishers.Add(
                new PublisherClass
                {
                    Name = $"{r[0]}",
                    Email = $"{r[1]}", // Read this from somewhere else?
                    Sent = $"{sent}"
                });
        }

        foreach (PublisherClass publisher in publishers)
        {
            Console.WriteLine($"Sending email to {publisher.Name}: {publisher.Email}: {publisher.Sent}...");

            if (string.IsNullOrWhiteSpace(publisher.Name))
            {
                Console.WriteLine($"Reached end of list at index {publishers.IndexOf(publisher)}");
                break;
            }

            publisher.Sent = DateTime.Now.ToString();

            string emailPattern = @"^\S+@\S+$";
            if (Regex.IsMatch(publisher.Email, emailPattern))
            {
                publisher.Result = "Sending";
                if (publisher.Email.ToUpper().EndsWith("@GMAIL.COM"))
                {
                    publisher.Result = "Preparing SMTP Email";
                    Message message = new()
                    {
                         ToAddress = publisher.Email,
                         ToName = publisher.Name,
                         Subject = "My Group CLM Schedule",
                         Text =  GenerateMessageText(publisher)
                    };

                    Simple.Send(message);

                    publisher.Result = "Sent via SMTP";
                }
                else
                {
                    try
                    {
                        publisher.Result = "Preparing SendMail Message";
                        Response response = SendGridEmailer.SendEmail(publisher.Name, publisher.Email, sendGridApiKey).Result;
                        Console.WriteLine($"SenndMail Status Code:{response.StatusCode}");
                        publisher.Result = $"SendMail Status Code:{response.StatusCode}";
                    }
                    catch (Exception ex)
                    {
                        publisher.Result = $"SendMail Error: {ex.Message}";
                    }
                }
            }
            else
            {
                publisher.Result = $"FAIL: not a valid email address";
            }
        }

        Console.WriteLine("Writing new values back");
        foreach (PublisherClass publisher in publishers)
        {
            rows[publishers.IndexOf(publisher)] = new object[4] { 
                publisher.Name, 
                publisher.Email, 
                publisher.Sent, 
                publisher.Result };
        }

        sheets.Write(
            documentId: documentId,
            range: range,
            values: rows);


    }

    public string GenerateMessageText(PublisherClass publisher)
    {
        scheduleViewSheet
    }
}

public class PublisherClass
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Sent { get; set; }
    public string? Result { get; set; }
}
}