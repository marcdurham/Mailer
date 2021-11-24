using GoogleAdapter.Adapters;
using SendGrid;
using System.Text.RegularExpressions;

namespace Mailer.Sender;
public class PublisherEmailer 
{
    public static void Run()
    {

        Console.WriteLine("Reading and writing to a Google spreadsheet...");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        // string json = Environment.GetEnvironmentVariable("ServiceSecretsJson", EnvironmentVariableTarget.Process)
        //     ?? throw new ArgumentNullException(nameof(json));
        string documentId = Environment.GetEnvironmentVariable("DocumentId", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(documentId));
        string range = Environment.GetEnvironmentVariable("Range", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(range));
        string sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process)
            ?? throw new ArgumentNullException(nameof(sendGridApiKey));
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        string json = File.ReadAllText("/app/SendGrid.secrets.json");
        string template = File.ReadAllText("./template1.html");

        var sheets = new Sheets(json, isServiceAccount: true);

        //IList<IList<object>> friendInfoRows = sheets.Read(documentId: documentId, range: "Friend Info!B1:AI500");

        //var friendList = new List<string>();
        //var friendMap = new Dictionary<string, string>();
        //foreach (var r in friendInfoRows)
        //{
        //    friendList.Add(r[0].ToString());
        //    friendMap[r[0].ToString()] = $"Name = {r[0]} Pinyin = {r[5]} CHS = {r[4]}";
        //}

        //Console.WriteLine();
        //Console.WriteLine("Friends:");
        //foreach(string friend in friendList)
        //{
        //    Console.WriteLine($"{friend}: {friendMap[friend]}");
        //}

        IList<IList<object>> clmAssignmentRows = sheets.Read(documentId: documentId, range: range);

        var publishers = new List<PublisherClass>();
        foreach (var r in clmAssignmentRows)
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
                        Text = new MailerCommon.ClmScheduleGenerator().Generate(
                             secretsJsonPath: "/app/SendGrid.secrets.json",
                             documentId: documentId,
                             range: "CLM Assignment List!B1:AY200",
                             friendName: publisher.Name,
                             template: template)
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
            clmAssignmentRows[publishers.IndexOf(publisher)] = new object[4] { 
                publisher.Name, 
                publisher.Email, 
                publisher.Sent, 
                publisher.Result };
        }

        sheets.Write(
            documentId: documentId,
            range: range,
            values: clmAssignmentRows);


    }
}

public class PublisherClass
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Sent { get; set; }
    public string? Result { get; set; }
}