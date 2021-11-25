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
        string? googleApiSecretsJson,
        bool dryRunMode = false)
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
            new List<IEmailSender>
            {
                new SaveEmailToFileEmailSender() { SendByDefault = dryRunMode },
                new SmtpEmailSender(isSender: m => m.ToAddress.ToUpper().EndsWith("@GMAIL.COM")),
                new SendGridEmailSender(sendGridApiKey) { SendByDefault = true }
            });

        string template = File.ReadAllText(ClmTemplatePath);

        bool isServiceAccount = IsJsonForAServiceAccount(googleApiSecretsJson);

        var sheets = new Sheets(googleApiSecretsJson, isServiceAccount: isServiceAccount);

        IList<IList<object>> clmSendEmailsRows = sheets.Read(
            documentId: clmSendEmailsDocumentId,
            range: ClmSendEmailsRange);

        List<EmailRecipient> recipients = ConvertToEmailRecipients(clmSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Friends:");
        var friendMap = ClmScheduleGenerator.GetFriends(sheets, clmAssignmentListDocumentId);
        foreach (string friend in friendMap.Keys)
        {
            Console.WriteLine($"{friend}: {friendMap[friend.ToUpperInvariant()]}");
        }

        var schedule = ClmScheduleGenerator.GetSchedule(sheets, clmAssignmentListDocumentId);

        foreach (EmailRecipient recipient in recipients)
        {
            Console.WriteLine($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");

            recipient.Sent = DateTime.Now.ToString();

            string nextMeetingDate = schedule.NextMeetingDate.ToString(IsoDateFormat);
            string subject = $"Eastside Christian Life and Ministry Assignments for {nextMeetingDate}";
           
            recipient.Result = "Sending";
            string htmlMessageText = ClmScheduleGenerator.Generate(
                    friendName: recipient.Name,
                    template: template,
                    friendMap: friendMap,
                    schedule: schedule);

            EmailMessage message = new()
            {
                FromAddress = "some@email.com",
                FromName = "My City My Group Information Board",
                ToAddress = recipient.EmailAddress!,
                ToName = recipient.Name,
                Subject = subject,
                Text = htmlMessageText
            };

            emailSender.Send(message);
        }

        Console.WriteLine("Writing new values back");
        foreach (EmailRecipient publisher in recipients)
        {
            clmSendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        sheets.Write(
            documentId: clmSendEmailsDocumentId,
            range: ClmSendEmailsRange,
            values: clmSendEmailsRows);
    }

    private static List<EmailRecipient> ConvertToEmailRecipients(IList<IList<object>> clmSendEmailsRows)
    {
        var tasks = new List<EmailRecipient>();
        foreach (IList<object> row in clmSendEmailsRows)
        {
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0].ToString()))
                break; // End of list

            string? email = row.Count > 1 ? $"{row[1]}" : null;
            string? sent = row.Count > 2 ? $"{row[2]}" : null;
            tasks.Add(
                new EmailRecipient
                {
                    Name = $"{row[0]}",
                    EmailAddress = email, // TODO: Get email address from somewhere else?
                    Sent = $"{sent}"
                });
        }

        return tasks;
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
