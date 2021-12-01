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

        var toSend = new List<ScheduleInputs>()
        {
            new ScheduleInputs()
            {
                MeetingName = "CLM",
                HtmlTemplate = clmTemplate,
                SendEmailsDocumentId = clmSendEmailsDocumentId,
                SendEmailsRange = "CLM Send Emails!B2:F300",
                AssignmentListDocumentId = clmAssignmentListDocumentId,
                SendDayOfWeek = DayOfWeek.Monday,
            },
            new ScheduleInputs()
            {
                MeetingName = "PW",
                HtmlTemplate = pwTemplate,
                SendEmailsDocumentId = pwSendEmailsDocumentId,
                SendEmailsRange = "PW Send Emails!B2:F300",
                AssignmentListDocumentId = pwAssignmentListDocumentId,
                SendDayOfWeek = DayOfWeek.Wednesday,
            },
            new ScheduleInputs()
            {
                MeetingName = "MFS",
                HtmlTemplate = mfsTemplate,
                SendEmailsDocumentId = mfsSendEmailsDocumentId,
                SendEmailsRange = "Service Send Emails!B2:F300",
                AssignmentListDocumentId = mfsAssignmentListDocumentId,
                SendDayOfWeek = DayOfWeek.Sunday,
            }
        };

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

        for (int w = 0; w < 4; w++)
        {
            DateTime monday = thisMonday.AddDays(w * 7);
            var week = new ScheduleWeek
            {
                Start = monday,
            };

            schedule.Weeks.Add(week);
        }
        
        SendShedulesFor("CLM", clmSendEmailsDocumentId, ClmSendEmailsRange, clmAssignmentListDocumentId, $"CLM Assignment List!B1:AY9999", clmTemplate, friendMap, thisMonday, schedule, DayOfWeek.Monday, DayOfWeek.Thursday);
        SendShedulesFor("PW", pwSendEmailsDocumentId, PwSendEmailsRange, pwAssignmentListDocumentId, "PW Assignment List!B1:AY9999", pwTemplate, friendMap, thisMonday, schedule, DayOfWeek.Wednesday, DayOfWeek.Saturday);
        SendShedulesFor("MFS", mfsSendEmailsDocumentId, MfsSendEmailsRange, mfsAssignmentListDocumentId, "Service Schedule!B1:AY9999", mfsTemplate, friendMap, thisMonday, schedule, DayOfWeek.Sunday, (DayOfWeek)0);

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    private void SendShedulesFor(string? meetingName, string? sendEmailsDocumentId, string? sendEmailsRange, string? assignmentListDocumentId, string? assignmentListRange, string htmlTemplate, Dictionary<string, Friend> friendMap, DateTime thisMonday, Schedule schedule, DayOfWeek sendDayOfWeek, DayOfWeek meetingDayOfWeek)
    {
        Console.WriteLine();
        Console.WriteLine($"Loading {meetingName} Email Recipients...");
        IList<IList<object>> sendEmailsRows = _sheets.Read(sendEmailsDocumentId, sendEmailsRange);
        List<EmailRecipient> recipients = EmailRecipientLoader.ConvertToEmailRecipients(sendEmailsRows);

        foreach (EmailRecipient recipient in recipients)
        {
            recipient.EmailAddressFromFriend = "Friend Not Found";

            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                if (string.IsNullOrWhiteSpace(recipient.EmailAddress))
                    recipient.EmailAddress = friend.EmailAddress;

                recipient.Result = string.Equals(
                    recipient.EmailAddress,
                    friend.EmailAddress,
                    StringComparison.OrdinalIgnoreCase)
                    ? "Friend Email Match" : "Friend Email Different";
            }
            else
            {
                recipient.Friend = new MissingFriend(recipient.Name);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Writing status of {meetingName} emails to recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            sendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: sendEmailsDocumentId,
            range: sendEmailsRange,
            values: sendEmailsRows);

        Console.WriteLine();
        Console.WriteLine($"Loading Assignment List for {meetingName}...");
        IList<IList<object>> values = _sheets.Read(documentId: assignmentListDocumentId, range: assignmentListRange);
        List<Meeting> meetings = ScheduleLoader.GetSchedule(values, friendMap, new int[] { (int)meetingDayOfWeek }, meetingName);
        foreach (Meeting meeting in meetings.Where(m => m.Date >= thisMonday))
        {
            // TODO: Make this day of the week agnostic, in case the day moves
            var week = schedule.Weeks.SingleOrDefault(w => meeting.Date >= w.Start && meeting.Date < w.Start.AddDays(7));

            if (week != null)
            {
                switch (meeting.Name)
                {
                    case "CLM":
                        week.Midweek = meeting;
                        break;
                    case "PW":
                        week.Weekend = meeting;
                        break;
                    case "MFS":
                        week.MeetingsForService.Add(meeting.Date, meeting);
                        break;
                    default:
                        week.MeetingsForService.Add(meeting.Date, meeting);
                        break;
                }
            }
            
        }

        Console.WriteLine();
        Console.WriteLine($"Generating HTML {meetingName} schedules and sending {meetingName} emails...");
        List<Meeting> allMeetings = schedule.AllMeetings()
            .Where(m => m.Date >= thisMonday && m.Date <= thisMonday.AddDays(35) && m.Name == meetingName)
            .OrderBy(m => m.Date)
            .ToList();

        string html = HtmlScheduleGenerator.Generate(
            template: htmlTemplate,
            meetings: allMeetings);

        Console.WriteLine();
        Console.WriteLine($"Sending {meetingName} schedules and setting status...");
        foreach (EmailRecipient recipient in recipients)
            GenerateAndSendEmailFor(html, schedule, allMeetings, recipient, sendDayOfWeek);


        Console.WriteLine();
        Console.WriteLine($"Writing status of emails to {meetingName} recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            sendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        _sheets.Write(
            documentId: sendEmailsDocumentId,
            range: sendEmailsRange,
            values: sendEmailsRows);
    }

    void GenerateAndSendEmailFor(
        string htmlMessageText, 
        Schedule schedule, 
        IEnumerable<Meeting> meetings, 
        EmailRecipient recipient,
        DayOfWeek sendDayOfWeek)
    {
        htmlMessageText = HtmlScheduleGenerator.Highlight(recipient.Friend, htmlMessageText);

        htmlMessageText = HtmlScheduleGenerator.InjectUpcomingAssignments(
            friendName: recipient.Name,
            friend: recipient.Friend,
            template: htmlMessageText,
            schedule: schedule);

        string nextMeetingDate = meetings.Min(m => m.Date).ToString(IsoDateFormat);
        string subject = $"Eastside {meetings.First().Name} Assignments for {nextMeetingDate}";

        SendEmailFor(subject, htmlMessageText, recipient, sendDayOfWeek);
    }

    void SendEmailFor(string subject, string htmlMessageText, EmailRecipient recipient, DayOfWeek sendDayOfWeek)
    {
        Console.WriteLine($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");

        if(!DateTime.TryParse(recipient.Sent, out DateTime sent))
        {
            sent = DateTime.MinValue;
        }

        if(sent.AddDays(7) >= DateTime.Today 
            && DateTime.Today.DayOfWeek == sendDayOfWeek
            || sent.AddDays(8) >= DateTime.Today)
        {
            recipient.Result = "Skipped: Sent Too Recently";
            return;
        }

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

        var result = _emailSender.Send(message);

        recipient.Sent = result.EmailWasSent ? DateTime.Now.ToString() : null;
        recipient.Result = result.Status;
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
