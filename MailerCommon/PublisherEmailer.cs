using GoogleAdapter.Adapters;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using MailerCommon;
using MailerCommon.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    const string IsoDateFormat = "yyyy-MM-dd";
    
    readonly IEmailSender _emailSender;
    readonly ScheduleOptions _scheduleOptions;
    readonly ICustomLogger<PublisherEmailer> _logger;
    readonly IMemoryCache _memoryCache;
    readonly ISheets _sheets;
    readonly Dictionary<string, List<Assignment>> friendCalendars = new Dictionary<string, List<Assignment>>();
    readonly double _timeZoneOffsetHours = 0.0;

    public PublisherEmailer(
        ScheduleOptions scheduleOptions,
        ICustomLogger<PublisherEmailer> logger, 
        IMemoryCache memoryCache, 
        ISheets sheets, 
        string? sendGridApiKey, 
        bool dryRunMode =  false, 
        bool forceSendAll = false)
    {
        _scheduleOptions = scheduleOptions;
        _logger = logger;
        _memoryCache = memoryCache;
        _sheets = sheets;

        if (sendGridApiKey == null)
            throw new ArgumentNullException(nameof(sendGridApiKey));

        if (string.IsNullOrWhiteSpace(scheduleOptions.TimeZone))
            throw new ArgumentNullException(nameof(scheduleOptions.TimeZone));

        _emailSender = new EmailSenderProxy(
            new List<IEmailSender>
            {
                new SaveEmailToFileEmailSender() { SendByDefault = dryRunMode },
                new SmtpEmailSender(isSender: m => m.ToAddress.ToUpper().EndsWith("@GMAIL.COM")),
                new SendGridEmailSender(sendGridApiKey, _scheduleOptions) { SendByDefault = true },
            });
        
        ForceSendAll = forceSendAll;

        _timeZoneOffsetHours = scheduleOptions.TimeZoneOffsetHours ?? 0.0;
    }

    public bool ForceSendAll { get; set; }

    public void Run(
        string? friendInfoDocumentId,
        List<ScheduleInputs> schedules)
    {
        if (friendInfoDocumentId == null)
            throw new ArgumentNullException(nameof(friendInfoDocumentId));

        friendCalendars.Clear();

        _logger.LogInformation("Loading Friends...");
        IList<IList<object>> friendInfoRows = _sheets.Read(documentId: friendInfoDocumentId, range: "Friend Info!B1:Z500");
        var friendMap = FriendLoader.GetFriends(friendInfoRows);
        _logger.LogInformation($"{friendMap.Count} Friends Loaded");

        _logger.LogInformation($"Sending {schedules.Count} schedules...");
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
       
        foreach(ScheduleInputs schedule in schedules)
        {
            SendSchedulesFor(schedule, friendMap, thisMonday);
        }

        foreach(var calendar in friendCalendars)
        {
            CacheFriendAssignments(calendar.Key, calendar.Value);
        }

        _logger.LogInformation("Done");
    }

    void SendSchedulesFor(
        ScheduleInputs scheduleInputs, 
        Dictionary<string, Friend> friendMap, 
        DateTime thisMonday)
    {
        string htmlTemplate = File.ReadAllText(scheduleInputs.HtmlTemplatePath);

        _logger.LogInformation($"Loading {scheduleInputs.MeetingName} Email Recipients...");
        IList<IList<object>> emailRecipientRows = _sheets.Read(scheduleInputs.EmailRecipientsDocumentId, scheduleInputs.EmailRecipientsRange);
        List<EmailRecipient> recipients = EmailRecipientLoader.ConvertToEmailRecipients(emailRecipientRows);

        foreach (EmailRecipient recipient in recipients)
        {
            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                if (string.IsNullOrWhiteSpace(recipient.EmailAddress))
                    recipient.EmailAddress = friend.EmailAddress;

                recipient.Check = $"{DateTime.Now}";
                recipient.CheckStatus = string.Equals(
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

        _logger.LogInformation($"Writing status of {scheduleInputs.MeetingName} emails to recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            emailRecipientRows[recipients.IndexOf(publisher)] = new object[6] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.SentStatus,
                $"{DateTime.Now}:",
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: scheduleInputs.EmailRecipientsDocumentId,
            range: scheduleInputs.EmailRecipientsRange,
            values: emailRecipientRows);

        _logger.LogInformation($"Loading Assignment List for {scheduleInputs.MeetingName}...");
        IList<IList<object>> values = _sheets.Read(documentId: scheduleInputs.AssignmentListDocumentId, range: scheduleInputs.AssignmentListRange);
        List<Meeting> meetings = ScheduleLoader.GetSchedule(
            values, 
            friendMap, 
            new int[] { (int)scheduleInputs.MeetingDayOfWeek }, 
            scheduleInputs.MeetingName,
            scheduleInputs.MeetingTitle,
            scheduleInputs.MeetingStartTime.HasValue 
                ? TimeOnly.FromDateTime((DateTime)scheduleInputs.MeetingStartTime) 
                : null,
            mondayColumnIndex: 0,
            meetingDateColumnIndex: scheduleInputs.MeetingDateColumnIndex ?? 0);

        _logger.LogInformation($"Generating HTML {scheduleInputs.MeetingName} schedules and sending {scheduleInputs.MeetingName} emails...");
        List<Meeting> upcomingMeetings = meetings
            .Where(m => m.Date >= thisMonday && m.Date <= thisMonday.AddDays(35) && m.Name == scheduleInputs.MeetingName)
            .OrderBy(m => m.Date)
            .ToList();

        string html = HtmlScheduleGenerator.Generate(
            html: htmlTemplate,
            meetings: upcomingMeetings);

        string schedulePath = Path.Combine(
            _scheduleOptions.StaticScheduleRootFolder, 
            scheduleInputs.MeetingName.ToLower());

        _logger.LogInformation($"Saving master copy {scheduleInputs.MeetingName} schedule...");
        File.WriteAllText($"{schedulePath}.html", html, Encoding.UTF8);

        string printTemplatePath = scheduleInputs.HtmlTemplatePath.Replace(".html", ".print.html");
        if (File.Exists(printTemplatePath))
        {
            string htmlPrintTemplate = File.ReadAllText(printTemplatePath);
            string htmlPrint = HtmlScheduleGenerator.Generate(
                       html: htmlPrintTemplate,
                       meetings: upcomingMeetings);
            _logger.LogInformation($"Saving print copy {scheduleInputs.MeetingName} schedule...");
            File.WriteAllText($"{schedulePath}.print.html", htmlPrint, Encoding.UTF8);
        }
        else
        {
            _logger.LogInformation($"No print template '{printTemplatePath}' was found.");
        }

        _logger.LogInformation($"Sending {scheduleInputs.MeetingName} schedules and setting status...");
        foreach (EmailRecipient recipient in recipients)
            GenerateAndSendEmailFor(html, upcomingMeetings, meetings, recipient, scheduleInputs);

        _logger.LogInformation($"Writing status of emails to {scheduleInputs.MeetingName} recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            emailRecipientRows[recipients.IndexOf(publisher)] = new object[6] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.SentStatus,
                publisher.Check,
                publisher.CheckStatus };
        }

        _sheets.Write(
            documentId: scheduleInputs.EmailRecipientsDocumentId,
            range: scheduleInputs.EmailRecipientsRange,
            values: emailRecipientRows);
    }

    void GenerateAndSendEmailFor(
        string htmlMessageText, 
        IEnumerable<Meeting> meetings, 
        IEnumerable<Meeting> allMeetings, 
        EmailRecipient recipient,
        ScheduleInputs scheduleInputs)
    {
        DayOfWeek sendDayOfWeek = scheduleInputs.SendDayOfWeek;

        htmlMessageText = HtmlScheduleGenerator.Highlight(recipient.Friend, htmlMessageText);

        (htmlMessageText, List<Assignment> friendAssignments) = HtmlScheduleGenerator.InjectUpcomingAssignments(
            friendName: recipient.Name,
            friend: recipient.Friend,
            template: htmlMessageText,
            meetings: meetings,
            allMeetings: allMeetings);

        string nextMeetingDate = meetings.Min(m => m.Date).ToString(IsoDateFormat);
        string subject = $"Eastside {meetings.First().Name} Assignments for {nextMeetingDate}";

        //CacheFriendAssignments(scheduleInputs.MeetingName, recipient, friendAssignments);

        string friendKey = $"{recipient.Name.ToUpper()}";
        if (!friendCalendars.ContainsKey(friendKey))
        {
            friendCalendars.Add(friendKey, friendAssignments);
        }
        else
        {
            friendCalendars[friendKey].AddRange(friendAssignments);
        }

        SendEmailFor(subject, htmlMessageText, recipient, sendDayOfWeek);
    }

    private void CacheFriendAssignments(string friendKey, List<Assignment> friendAssignments)
    {
        var shortCalendar = new Ical.Net.Calendar();

        foreach (Assignment assignment in friendAssignments)
        {
            string assignmentName = assignment.Name;
            if (assignmentName.Contains(" - "))
                assignmentName = string.Join("-", assignmentName.Split("-", StringSplitOptions.RemoveEmptyEntries).Reverse());

            DateTime start = assignment.Date;
            if(!assignment.Start.Equals(TimeOnly.MinValue))
            {
                start = start.Add(assignment.Start.ToTimeSpan());
            }

            var calEvent = new CalendarEvent
            {
                Start = new CalDateTime(start, _scheduleOptions.TimeZone),
                Summary = $"{assignmentName} ({assignment.Meeting})",
            };

            shortCalendar.Events.Add(calEvent);
        }

        var serializer = new CalendarSerializer();
        var serializedCalendar = serializer.SerializeToString(shortCalendar);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(60 * 60)); // One hour

        _memoryCache.Set(friendKey, serializedCalendar, cacheEntryOptions);
    }

    void SendEmailFor(string subject, string htmlMessageText, EmailRecipient recipient, DayOfWeek sendDayOfWeek)
    {
        if(!DateTime.TryParse(recipient.Sent, out DateTime sent))
        {
            sent = DateTime.MinValue;
        }

        if(!ForceSendAll && (sent.AddDays(7) >= DateTime.Today 
            && DateTime.Today.DayOfWeek == sendDayOfWeek
            || sent.AddDays(8) >= DateTime.Today))
        {
            recipient.Check = $"{DateTime.Now}";
            recipient.CheckStatus = "Skipped: Sent too recently";
            return; 
        }

        _logger.LogInformation($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");
        recipient.Check = $"{DateTime.Now}";
        recipient.CheckStatus = "Sending...";

        EmailMessage message = new()
        {
            FromAddress = _scheduleOptions.EmailFromAddress ?? "auto@mailer.org",
            FromName = _scheduleOptions.EmailFromName ?? "Mailer Information Board",
            ToAddress = recipient.EmailAddress!,
            ToName = recipient.Name,
            Subject = subject,
            Text = htmlMessageText
        };

        var result = _emailSender.Send(message);

        var now = DateTime.Now;
        recipient.Sent = now.ToString();
        recipient.SentStatus = result.Status;
        recipient.Check = now.ToString();
        recipient.CheckStatus = result.Status;
    }
}
