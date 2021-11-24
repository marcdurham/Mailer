using GoogleAdapter.Adapters;
using SendGrid;
using System.Text.RegularExpressions;

namespace Mailer.Sender
{
public class PublisherEmailer 
{
    public static void Run(
        string? clmSendEmailsDocumentId, 
        string clmAssignmentListDocumentId, 
        string? range, 
        string? sendGridApiKey, string? googleApiSecretsJson)
        {
        if(clmSendEmailsDocumentId == null)
            throw new ArgumentNullException(nameof(clmSendEmailsDocumentId));

        if (range == null)
            throw new ArgumentNullException(nameof(range));
        
        if (sendGridApiKey == null)
            throw new ArgumentNullException(nameof(sendGridApiKey));

        if (googleApiSecretsJson == null)
                throw new ArgumentNullException(nameof(googleApiSecretsJson));

        string clmSendEmailsRange = "CLM Send Emails!B2:E300";

        string template = File.ReadAllText("./template1.html");

        var sheets = new Sheets(googleApiSecretsJson, isServiceAccount: false);

        IList<IList<object>> clmSendEmailsRows = sheets.Read(
            documentId: clmSendEmailsDocumentId, 
            range: clmSendEmailsRange);

        var publishers = new List<PublisherClass>();
        foreach (var r in clmSendEmailsRows)
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

        var friendMap = MailerCommon.ClmScheduleGenerator.GetFriends(sheets, clmAssignmentListDocumentId);
        var schedule = MailerCommon.ClmScheduleGenerator.GetSchedule(sheets, clmAssignmentListDocumentId);

        foreach (PublisherClass publisher in publishers)
        {
            Console.WriteLine($"Sending email to {publisher.Name}: {publisher.Email}: {publisher.Sent}...");

            if (string.IsNullOrWhiteSpace(publisher.Name))
            {
                Console.WriteLine($"Reached end of list at index {publishers.IndexOf(publisher)}");
                break;
            }

            publisher.Sent = DateTime.Now.ToString();

            string nextMeetingDate = schedule.NextMeetingDate.ToString("yyyy-MM-dd");
            string subject = $"Eastside Christian Life and Ministry Assignments for {nextMeetingDate}";
            string emailPattern = @"^\S+@\S+$";
            if (Regex.IsMatch(publisher.Email, emailPattern))
            {
                publisher.Result = "Sending";
                string htmlMessageText = new MailerCommon.ClmScheduleGenerator().Generate(
                        sheets: sheets,
                        googleApiSecretsJson: googleApiSecretsJson,
                        documentId: clmAssignmentListDocumentId,
                        range: "CLM Assignment List!B1:AY200",
                        friendName: publisher.Name,
                        template: template,
                        friendMap: friendMap,
                        schedule: schedule);

                if (publisher.Email.ToUpper().EndsWith("@GMAIL.COM-XXXXXXXXXXXXXXXXXXXXXXX"))
                {
                    publisher.Result = "Preparing SMTP Email";
                        Message message = new()
                        {

                            ToAddress = publisher.Email,
                            ToName = publisher.Name,
                            Subject = subject,
                            Text = htmlMessageText
                        };

                        // TODO: uncomment this:
                        Simple.Send(message);
                        File.WriteAllText($"{publisher.Name}.{publisher.Email}.{subject.Replace(":","")}.html", htmlMessageText);

                    publisher.Result = "Sent via SMTP";
                }
                else
                {
                    try
                    {
                        publisher.Result = "Preparing SendMail Message";
                        File.WriteAllText($"{publisher.Name}.{publisher.Email}.{subject.Replace(":", "")}.html", htmlMessageText);
                        Response response = SendGridEmailer.SendEmail(publisher.Name, publisher.Email, sendGridApiKey, subject, htmlMessageText).Result;
                        Console.WriteLine($"SenndMail Status Code:{response.StatusCode}");
                        publisher.Result = $"SendMail Status Code:{response.StatusCode}";
                        
                        //publisher.Result = $"SendMail Not really sent";
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
            clmSendEmailsRows[publishers.IndexOf(publisher)] = new object[4] { 
                publisher.Name, 
                publisher.Email, 
                publisher.Sent, 
                publisher.Result };
        }

        sheets.Write(
            documentId: clmSendEmailsDocumentId,
            range: clmSendEmailsRange,
            values: clmSendEmailsRows);


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