using GoogleAdapter.Adapters;
using MailerCommon;
using System.Text.Json;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    private const string ClmAssignmentListRange = "CLM Assignment List!B1:AY200";
    private const string ClmSendEmailsRange = "CLM Send Emails!B2:F300";
    private const string ClmTemplatePath = "./template1.html";
    private const string PwTemplatePath = "./template3.html";
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

        string clmTemplate = File.ReadAllText(ClmTemplatePath);
        string pwTemplate = File.ReadAllText(PwTemplatePath);

        bool isServiceAccount = IsJsonForAServiceAccount(googleApiSecretsJson);

        var sheets = new Sheets(googleApiSecretsJson, isServiceAccount: isServiceAccount);

        Console.WriteLine();
        Console.WriteLine("Loading Email Recipients...");
        IList<IList<object>> clmSendEmailsRows = sheets.Read(clmSendEmailsDocumentId, ClmSendEmailsRange);
        List<EmailRecipient> recipients = EmailRecipientLoader.ConvertToEmailRecipients(clmSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading Friends...");
        IList<IList<object>> friendInfoRows = sheets.Read(documentId: clmAssignmentListDocumentId, range: "Friend Info!B1:AI500");
        var friendMap = FriendLoader.GetFriends(friendInfoRows);
        foreach (string friend in friendMap.Keys)
        {
            Console.WriteLine($"{friend}: {friendMap[friend.ToUpperInvariant()]}");
        }

        foreach(EmailRecipient recipient in recipients)
        {
            recipient.EmailAddressFromFriend = friendMap.ContainsKey(recipient.Name.ToUpperInvariant()) 
                ? friendMap[recipient.Name.ToUpperInvariant()].EmailAddress 
                : "Friend Not Found";
        }

        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        var schedule = new Schedule()
        {
            NextMeetingDate = thisMonday,   // different
        };

        for(int w = 0; w < 4; w++)
        {
            DateTime monday = thisMonday.AddDays(w * 7);
            var week = new ScheduleWeek
            {
                Start = monday,
                //Midweek = meeting // different
            };

            schedule.Weeks.Add(week);
        }

        Console.WriteLine();
        Console.WriteLine("Loading Assignment List for CLM...");
        IList<IList<object>> values = sheets.Read(documentId: clmAssignmentListDocumentId, range: "CLM Assignment List!B1:AY9999");
        List<Meeting> clmMeetings = ScheduleLoader.GetSchedule(values, friendMap, 3, "CLM");
        foreach(Meeting meeting in clmMeetings)
        {
            var week = schedule.Weeks.SingleOrDefault(w => w.Start.AddDays(3) == meeting.Date);
            if(week != null)
                week.Midweek = meeting;
        }

        Console.WriteLine();
        Console.WriteLine("Loading Assignment List for PW...");
        IList<IList<object>> pwValues = sheets.Read(documentId: clmAssignmentListDocumentId, range: "PW Assignment List!B1:AM9999");
        List<Meeting> pwMeetings = ScheduleLoader.GetSchedule(pwValues, friendMap, 5, "PW");
        foreach (Meeting meeting in pwMeetings)
        {
            var week = schedule.Weeks.SingleOrDefault(w => w.Start.AddDays(5) == meeting.Date);
            if (week != null)
                week.Weekend = meeting;
        }

        Console.WriteLine();
        Console.WriteLine("Sending CLM Emails...");
        foreach (EmailRecipient recipient in recipients)
        {
            List<Meeting> meetings = schedule.AllMeetings()
                .Where(m => m.Date >= thisMonday && m.Date.DayOfWeek == DayOfWeek.Thursday)
                .OrderBy(m => m.Date)
                .ToList();

            SendEmailFor(emailSender, clmTemplate, friendMap, meetings, schedule, recipient);
        }

        Console.WriteLine();
        Console.WriteLine("Sending PW Emails...");
        foreach (EmailRecipient recipient in recipients)
        {
            List<Meeting> meetings = schedule.AllMeetings()
                .Where(m => m.Date >= thisMonday && m.Date.DayOfWeek == DayOfWeek.Saturday)
                .OrderBy(m => m.Date)
                .ToList();

            SendEmailFor(emailSender, pwTemplate, friendMap, meetings, schedule, recipient);
        }

        Console.WriteLine();
        Console.WriteLine("Writing status of emails to recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            clmSendEmailsRows[recipients.IndexOf(publisher)] = new object[5] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.EmailAddressFromFriend,
                publisher.Sent,
                publisher.Result };
        }

        sheets.Write(
            documentId: clmSendEmailsDocumentId,
            range: ClmSendEmailsRange,
            values: clmSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    private static void SendEmailFor(IEmailSender emailSender, string template, Dictionary<string, Friend> friendMap, List<Meeting> meetings, Schedule schedule, EmailRecipient recipient)
    {
        Console.WriteLine($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");

        recipient.Sent = DateTime.Now.ToString();
        
        string nextMeetingDate = meetings.Min(m => m.Date).ToString(IsoDateFormat);
        string subject = $"Eastside {meetings.First().Name} Assignments for {nextMeetingDate}";

        recipient.Result = "Sending";
        string htmlMessageText = ClmScheduleGenerator.Generate(
                friendName: recipient.Name,
                template: template,
                friendMap: friendMap,
                meetings: meetings,
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

    static bool IsJsonForAServiceAccount(string? json)
    {
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            MaxDepth = 3
        };

        JsonDocument document = JsonDocument.Parse(json, options);

        bool isServiceAccount;
        if (document.RootElement.TryGetProperty("type", out JsonElement element) 
            && element.GetString() == "service_account")
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
