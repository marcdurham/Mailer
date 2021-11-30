using GoogleAdapter.Adapters;
using MailerCommon;
using System.Text.Json;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    private const string ClmSendEmailsRange = "CLM Send Emails!B2:F300";
    private const string PwSendEmailsRange = "PW Send Emails!B2:F300";
    private const string MfsSendEmailsRange = "Service Send Emails!B2:F300";
    private const string ClmTemplatePath = "./template1.html";
    private const string PwTemplatePath = "./template3.html";
    private const string MfsTemplatePath = "./template4.html";
    private const string IsoDateFormat = "yyyy-MM-dd";
    readonly IEmailSender _emailSender;
    private readonly ISheets _sheets;

    public PublisherEmailer(ISheets sheets, string? sendGridApiKey, bool dryRunMode =  false)
    {
        _sheets = sheets;

        if (sendGridApiKey == null)
            throw new ArgumentNullException(nameof(sendGridApiKey));

        _emailSender = new EmailSenderProxy(
            new List<IEmailSender>
            {
                new SaveEmailToFileEmailSender() { SendByDefault = dryRunMode },
                new SmtpEmailSender(isSender: m => m.ToAddress.ToUpper().EndsWith("@GMAIL.COM")),
                new SendGridEmailSender(sendGridApiKey) { SendByDefault = true }
            });
    }

    public void Run(
        string? clmSendEmailsDocumentId, 
        string? clmAssignmentListDocumentId,
        string? pwSendEmailsDocumentId,
        string? pwAssignmentListDocumentId,
        string? mfsSendEmailsDocumentId,
        string? mfsAssignmentListDocumentId,
        string? friendInfoDocumentId)
    {
        if (clmSendEmailsDocumentId == null)
            throw new ArgumentNullException(nameof(clmSendEmailsDocumentId));
        if (pwAssignmentListDocumentId == null)
            throw new ArgumentNullException(nameof(pwAssignmentListDocumentId));
        if (clmAssignmentListDocumentId == null)
            throw new ArgumentNullException(nameof(clmAssignmentListDocumentId));
        if (mfsAssignmentListDocumentId == null)
            throw new ArgumentNullException(nameof(mfsAssignmentListDocumentId));
        if (friendInfoDocumentId == null)
            throw new ArgumentNullException(nameof(friendInfoDocumentId));

        string clmTemplate = File.ReadAllText(ClmTemplatePath);
        string pwTemplate = File.ReadAllText(PwTemplatePath);
        string mfsTemplate = File.ReadAllText(MfsTemplatePath);

        Console.WriteLine();
        Console.WriteLine("Loading Friends...");
        IList<IList<object>> friendInfoRows = _sheets.Read(documentId: friendInfoDocumentId, range: "Friend Info!B1:Z500"); //"Friend Info!B1:AI500");
        var friendMap = FriendLoader.GetFriends(friendInfoRows);
        foreach (string friend in friendMap.Keys)
        {
            Console.WriteLine($"{friend}: {friendMap[friend.ToUpperInvariant()]}");
        }

        Console.WriteLine();
        Console.WriteLine("Building schedule...");
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        var schedule = new Schedule()
        {
            NextMeetingDate = thisMonday,
        };

        for(int w = 0; w < 4; w++)
        {
            DateTime monday = thisMonday.AddDays(w * 7);
            var week = new ScheduleWeek
            {
                Start = monday,
            };

            schedule.Weeks.Add(week);
        }

        Console.WriteLine();
        Console.WriteLine("Loading CLM Email Recipients...");
        IList<IList<object>> clmSendEmailsRows = _sheets.Read(clmSendEmailsDocumentId, ClmSendEmailsRange);
        List<EmailRecipient> clmRecipients = EmailRecipientLoader.ConvertToEmailRecipients(clmSendEmailsRows);

        foreach (EmailRecipient recipient in clmRecipients)
        {
            recipient.EmailAddressFromFriend = "Friend Not Found";

            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                recipient.Result = string.Equals(
                    recipient.EmailAddress,
                    friend.EmailAddress,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Friend Email Match" : "Friend Email Different";
            }
        }

        Console.WriteLine();
        Console.WriteLine("Writing status of CLM emails to recipients...");
        foreach (EmailRecipient publisher in clmRecipients)
        {
            clmSendEmailsRows[clmRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: clmSendEmailsDocumentId,
            range: ClmSendEmailsRange,
            values: clmSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading Assignment List for CLM...");
        IList<IList<object>> values = _sheets.Read(documentId: clmAssignmentListDocumentId, range: "CLM Assignment List!B1:AY9999");
        List<Meeting> clmMeetings = ScheduleLoader.GetSchedule(values, friendMap, new int[] { 3 }, "CLM");
        foreach(Meeting meeting in clmMeetings.Where(m => m.Date >= thisMonday))
        {
            var week = schedule.Weeks.SingleOrDefault(w => w.Start.AddDays(3) == meeting.Date);
            if (week != null)
                week.Midweek = meeting;
        }

        Console.WriteLine();
        Console.WriteLine("Generating HTML CLM schedules and sending CLM emails...");
        List<Meeting> thursdayMeetings = schedule.AllMeetings()
            .Where(m => m.Date >= thisMonday && m.Name == "CLM")
            .OrderBy(m => m.Date)
            .Take(4)
            .ToList();

        string thursdayHtml = HtmlScheduleGenerator.Generate(
            template: clmTemplate,
            meetings: thursdayMeetings);

        Console.WriteLine();
        Console.WriteLine("Sending CLM schedules and setting status...");
        foreach (EmailRecipient recipient in clmRecipients)
            GenerateAndSendEmailFor(thursdayHtml, schedule, thursdayMeetings, recipient);


        Console.WriteLine();
        Console.WriteLine("Writing status of emails to CLM recipients...");
        foreach (EmailRecipient publisher in clmRecipients)
        {
            clmSendEmailsRows[clmRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        _sheets.Write(
            documentId: clmSendEmailsDocumentId,
            range: ClmSendEmailsRange,
            values: clmSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading PW Email Recipients...");
        IList<IList<object>> pwSendEmailsRows = _sheets.Read(pwSendEmailsDocumentId, PwSendEmailsRange);
        List<EmailRecipient> pwRecipients = EmailRecipientLoader.ConvertToEmailRecipients(pwSendEmailsRows);

        foreach (EmailRecipient recipient in pwRecipients)
        {
            recipient.EmailAddressFromFriend = "Friend Not Found";

            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                recipient.Result = string.Equals(
                    recipient.EmailAddress,
                    friend.EmailAddress,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Friend Email Match" : "Friend Email Different";
            }
        }

        Console.WriteLine();
        Console.WriteLine("Writing status of PW emails to recipients...");
        foreach (EmailRecipient publisher in pwRecipients)
        {
            pwSendEmailsRows[pwRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: pwSendEmailsDocumentId,
            range: PwSendEmailsRange,
            values: pwSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading Assignment List for PW...");
        IList<IList<object>> pwValues = _sheets.Read(documentId: pwAssignmentListDocumentId, range: "PW Assignment List!B1:AM9999");
        List<Meeting> pwMeetings = ScheduleLoader.GetSchedule(pwValues, friendMap, new int[] { 5 }, "PW");
        foreach (Meeting meeting in pwMeetings.Where(m => m.Date >= thisMonday))
        {
            var week = schedule.Weeks.SingleOrDefault(w => w.Start.AddDays(5) == meeting.Date);
            if (week != null)
                week.Weekend = meeting;
        }

        Console.WriteLine();
        Console.WriteLine("Generating HTML PW schedules and sending emails...");
        List<Meeting> saturdayMeetings = schedule.AllMeetings()
            .Where(m => m.Date >= thisMonday && m.Name == "PW")
            .OrderBy(m => m.Date)
            .Take(4)
            .ToList();
        
        string saturdayHtml = HtmlScheduleGenerator.Generate(
            template: pwTemplate,
            meetings: saturdayMeetings);

        Console.WriteLine();
        Console.WriteLine("Sending PW schedules and setting status...");
        foreach (EmailRecipient recipient in pwRecipients)
            GenerateAndSendEmailFor(saturdayHtml, schedule, saturdayMeetings, recipient);

        Console.WriteLine();
        Console.WriteLine("Writing status of emails to recipients...");
        foreach (EmailRecipient publisher in pwRecipients)
        {
            pwSendEmailsRows[pwRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        _sheets.Write(
            documentId: pwSendEmailsDocumentId,
            range: PwSendEmailsRange,
            values: pwSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading MFS Email Recipients...");
        IList<IList<object>> mfsSendEmailsRows = _sheets.Read(mfsSendEmailsDocumentId, MfsSendEmailsRange);
        List<EmailRecipient> mfsRecipients = EmailRecipientLoader.ConvertToEmailRecipients(mfsSendEmailsRows);

        foreach (EmailRecipient recipient in mfsRecipients)
        {
            recipient.EmailAddressFromFriend = "Friend Not Found";

            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                recipient.Result = string.Equals(
                    recipient.EmailAddress,
                    friend.EmailAddress,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Friend Email Match" : "Friend Email Different";
            }
        }

        Console.WriteLine();
        Console.WriteLine("Writing status of MFS emails to recipients...");
        foreach (EmailRecipient publisher in mfsRecipients)
        {
            clmSendEmailsRows[mfsRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: mfsSendEmailsDocumentId,
            range: MfsSendEmailsRange,
            values: mfsSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Loading Assignment List for MFS Schedule...");
        IList<IList<object>> mfsValues = _sheets.Read(documentId: mfsAssignmentListDocumentId, range: "Service Schedule!B1:L9999");
        List<Meeting> mfsMeetings = ScheduleLoader.GetSchedule(mfsValues, friendMap, new int[]{0}, "MFS");
        foreach (Meeting meeting in mfsMeetings.Where(m => m.Date >= thisMonday))
        {
            var week = schedule.Weeks.SingleOrDefault(w => meeting.Date >= w.Start
                && meeting.Date <= w.Start.AddDays(6));
            if (week != null)
                week.MeetingsForService.Add(meeting.Date, meeting);
        }

        Console.WriteLine();
        Console.WriteLine("Generating HTML MFS schedules and sending emails...");
        List<Meeting> allMfsMeetings = schedule.AllMeetings()
            .Where(m => m.Date >= thisMonday && m.Name == "MFS")
            .OrderBy(m => m.Date)
            .Take(7 * 2)
            .ToList();

        string mfsHtml = HtmlScheduleGenerator.Generate(
            template: mfsTemplate,
            meetings: allMfsMeetings);

        Console.WriteLine();
        Console.WriteLine("Sending MFS schedules and setting status...");
        foreach (EmailRecipient recipient in mfsRecipients)
            GenerateAndSendEmailFor(mfsHtml, schedule, allMfsMeetings, recipient);

        Console.WriteLine();
        Console.WriteLine("Writing status of emails to MFS recipients...");
        foreach (EmailRecipient publisher in mfsRecipients)
        {
            mfsSendEmailsRows[mfsRecipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        _sheets.Write(
            documentId: mfsSendEmailsDocumentId,
            range: MfsSendEmailsRange,
            values: mfsSendEmailsRows);

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    void GenerateAndSendEmailFor(
        string htmlMessageText, 
        Schedule schedule, 
        IEnumerable<Meeting> meetings, 
        EmailRecipient recipient)
    {
        htmlMessageText = HtmlScheduleGenerator.Highlight(recipient.Friend, htmlMessageText);

        htmlMessageText = HtmlScheduleGenerator.InjectUpcomingAssignments(
            friendName: recipient.Name,
            friend: recipient.Friend,
            template: htmlMessageText,
            schedule: schedule);

        string nextMeetingDate = meetings.Min(m => m.Date).ToString(IsoDateFormat);
        string subject = $"Eastside {meetings.First().Name} Assignments for {nextMeetingDate}";

        SendEmailFor(subject, htmlMessageText, recipient);
    }

    void SendEmailFor(string subject, string htmlMessageText, EmailRecipient recipient)
    {
        Console.WriteLine($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");

        recipient.Sent = DateTime.Now.ToString();
        recipient.Result = "Sending";

        EmailMessage message = new()
        {
            FromAddress = "some@email.com",
            FromName = "My City My Group Information Board",
            ToAddress = recipient.EmailAddress!,
            ToName = recipient.Name,
            Subject = subject,
            Text = htmlMessageText
        };

        _emailSender.Send(message);

        recipient.Result = "Sent";
    }

    public static bool IsJsonForAServiceAccount(string? json)
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
