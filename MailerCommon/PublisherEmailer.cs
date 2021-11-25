using GoogleAdapter.Adapters;
using MailerCommon;
using SendGrid;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    private const string ClmAssignmentListRange = "CLM Assignment List!B1:AY200";
    private const string ClmSendEmailsRange = "CLM Send Emails!B2:E300";
    private const string ClmTemplatePath = "./template1.html";
    private const string IsoDateFormat = "yyyy-MM-dd";

    public static void Run(
        string? clmSendEmailsDocumentId, 
        string? clmAssignmentListDocumentId, 
        string? sendGridApiKey, 
        string? googleApiSecretsJson)
        {
            if (clmSendEmailsDocumentId == null)
                throw new ArgumentNullException(nameof(clmSendEmailsDocumentId));
            if (clmAssignmentListDocumentId == null)
                throw new ArgumentNullException(nameof(clmAssignmentListDocumentId));
            if (sendGridApiKey == null)
                throw new ArgumentNullException(nameof(sendGridApiKey));
            if (googleApiSecretsJson == null)
                throw new ArgumentNullException(nameof(googleApiSecretsJson));

            IEmailSender emailSender = new EmailSenderProxy(
                new List<EmailSenderFunction>
                {
                    new (new SmtpEmailSender(), m => m.ToAddress?.ToUpper().EndsWith("@GMAIL.COM") ?? false),
                    new (new SendGridEmailSender(sendGridApiKey), (m) => true),
                    new (new FakeFileEmailSender(), m => true) // never gets here
                });

            string template = File.ReadAllText(ClmTemplatePath);

            bool isServiceAccount = IsJsonForAServiceAccount(googleApiSecretsJson);

            var sheets = new Sheets(googleApiSecretsJson, isServiceAccount: isServiceAccount);

            IList<IList<object>> clmSendEmailsRows = sheets.Read(
                documentId: clmSendEmailsDocumentId,
                range: ClmSendEmailsRange);

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

            var friendMap = ClmScheduleGenerator.GetFriends(sheets, clmAssignmentListDocumentId);
            var schedule = ClmScheduleGenerator.GetSchedule(sheets, clmAssignmentListDocumentId);

            foreach (PublisherClass publisher in publishers)
            {
                Console.WriteLine($"Sending email to {publisher.Name}: {publisher.Email}: {publisher.Sent}...");

                if (string.IsNullOrWhiteSpace(publisher.Name))
                {
                    Console.WriteLine($"Reached end of list at index {publishers.IndexOf(publisher)}");
                    break;
                }

                publisher.Sent = DateTime.Now.ToString();

                string nextMeetingDate = schedule.NextMeetingDate.ToString(IsoDateFormat);
                string subject = $"Eastside Christian Life and Ministry Assignments for {nextMeetingDate}";
                string emailPattern = @"^\S+@\S+$";
                if (Regex.IsMatch(publisher.Email, emailPattern))
                {
                    publisher.Result = "Sending";
                    string htmlMessageText = MailerCommon.ClmScheduleGenerator.Generate(
                            sheets: sheets,
                            googleApiSecretsJson: googleApiSecretsJson,
                            documentId: clmAssignmentListDocumentId,
                            range: ClmAssignmentListRange,
                            friendName: publisher.Name,
                            template: template,
                            friendMap: friendMap,
                            schedule: schedule);


                    EmailMessage message = new()
                    {
                        FromAddress = "some@email.com",
                        FromName = "My City My Group Information Board",
                        ToAddress = publisher.Email,
                        ToName = publisher.Name,
                        Subject = subject,
                        Text = htmlMessageText
                    };

                    if (publisher.Email.ToUpper().EndsWith("@GMAIL.COM"))
                    {
                        publisher.Result = "Preparing SMTP Email";

                        // TODO: uncomment this:
                        SmtpEmailSender.Send(message);
                        File.WriteAllText($"{publisher.Name}.{publisher.Email}.{subject.Replace(":", "")}.html", htmlMessageText);

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
                range: ClmSendEmailsRange,
                values: clmSendEmailsRows);
        }

        static bool IsJsonForAServiceAccount(string? googleApiSecretsJson)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                MaxDepth = 3
            };

            JsonDocument document = JsonDocument.Parse(googleApiSecretsJson, options);

            bool isServiceAccount;
            if (document.RootElement.TryGetProperty("type", out JsonElement element) && element.GetString() == "service_account")
            {
                isServiceAccount = true;
            }
            // This file looks like an OAuth 2.0 JSON file
            else if (document.RootElement.TryGetProperty("installed", out JsonElement installedElement)
                    && installedElement.TryGetProperty("redirect_uris", out _))
            {
                isServiceAccount = false;
            }
            else
            {
                throw new Exception("Unknown secrets json file type");
            }

            return isServiceAccount;
        }
    }

public class PublisherClass
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Sent { get; set; }
    public string? Result { get; set; }
}
