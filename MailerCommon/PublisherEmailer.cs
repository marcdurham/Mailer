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
        bool dryRunMode = false,
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

        if (scheduleOptions.TimeZoneOffsetHours == null)
            _logger.LogInformation($"TimeZoneOffsetHours is null");

        _timeZoneOffsetHours = scheduleOptions.TimeZoneOffsetHours ?? 0.0;
        _logger.LogInformation($"TimeZoneOffsetHours: {scheduleOptions.TimeZoneOffsetHours:0.0}");
    }

    public bool ForceSendAll { get; set; }
    DateTime _localNow = DateTime.MinValue;

    public void Run(
        DateTime utcNow,
        string? friendInfoDocumentId,
        List<ScheduleInputs> schedules)
    {
         _localNow = utcNow.AddHours(_timeZoneOffsetHours);

        if (friendInfoDocumentId == null)
            throw new ArgumentNullException(nameof(friendInfoDocumentId));

        friendCalendars.Clear();

        _logger.LogInformation("Loading Friends...");
        IList<IList<object>> friendInfoRows = _sheets.Read(documentId: friendInfoDocumentId, range: "Friend Info!B1:Z500");
        var friendMap = FriendLoader.GetFriends(friendInfoRows);
        _logger.LogInformation($"{friendMap.Count} Friends Loaded");

        _logger.LogInformation($"Sending {schedules.Count} schedules...");

// TODO: Save emails to a file or something, json mabye, separate email from sched generation
        foreach (ScheduleInputs schedule in schedules)
        {
            SendSchedulesFor(schedule, friendMap, _localNow);
        }

        foreach (var calendar in friendCalendars)
        {
            CacheFriendAssignments(calendar.Key, calendar.Value);
        }

        _logger.LogInformation("Done");
    }

    /// <summary>
    /// Returns the first day of the given week, which starts on Monday
    /// </summary>
    /// <param name="now">Current date</param>
    /// <returns></returns>
    public static DateTime GetMonday(DateTime now)
    {
        return now.Date.AddDays(-(((int)now.Date.DayOfWeek + 6) % 7));
    }

        /// <summary>
    /// Returns the dayOfWeek for the given week, which starts on Monday
    /// </summary>
    /// <param name="now">Current date</param>
    /// <returns></returns>
    public static DateTime GetThisWeeks(DateTime now, DayOfWeek dayOfWeek)
    {
        return now.Date.AddDays(-(((int)now.Date.DayOfWeek + 6) % 7) + ((int)dayOfWeek) - 1);
    }

    void SendSchedulesFor(
        ScheduleInputs scheduleInputs,
        Dictionary<string, Friend> friendMap,
        DateTime now)
    {
        DateTime thisMonday = GetMonday(now);
        string htmlTemplate = File.ReadAllText(scheduleInputs.HtmlTemplatePath);

        _logger.LogInformation($"Loading {scheduleInputs.MeetingName} Email Recipients...");
        IList<IList<object>> emailRecipientRows = _sheets.Read(
            scheduleInputs.EmailRecipientsDocumentId, 
            scheduleInputs.EmailRecipientsRange);

        List<EmailRecipient> recipients = EmailRecipientLoader.ConvertToEmailRecipients(emailRecipientRows);

        foreach (EmailRecipient recipient in recipients)
        {
            if (friendMap.TryGetValue(recipient.Name.ToUpper(), out Friend friend))
            {
                recipient.Friend = friend;
                if (string.IsNullOrWhiteSpace(recipient.EmailAddress))
                    recipient.EmailAddress = friend.EmailAddress;

                recipient.Check = $"{_localNow}";
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
                $"{_localNow}:",
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
            .Where(m => m.Date >= now.Date && m.Date <= thisMonday.AddDays(35) && m.Name == scheduleInputs.MeetingName)
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

        _logger.LogInformation($"Writing status of emails to {scheduleInputs.MeetingName} recipients list...");
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
        Thread.Sleep(10_000); // sleep for 10 seconds
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
            if (!assignment.Start.Equals(TimeOnly.MinValue))
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
        if (!DateTime.TryParse(recipient.Sent, out DateTime sent))
        {
            sent = DateTime.MinValue;
        }

        if (!ForceSendAll && (sent.AddDays(7) >= _localNow
            && _localNow.DayOfWeek == sendDayOfWeek
            || sent.AddDays(8) >= _localNow))
        {
            recipient.Check = $"{_localNow}";
            recipient.CheckStatus = "Skipped: Sent too recently";
            return;
        }

        _logger.LogInformation($"Sending email to {recipient.Name}: {recipient.EmailAddress}: Subject: {subject}...");
        recipient.Check = $"{_localNow}";
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

        EmailSenderResult? result = _emailSender.Send(message);

        _logger.LogInformation($"Email result: Status: {result.Status} Recipient: {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");

        recipient.Sent = _localNow.ToString();
        recipient.SentStatus = result.Status;
        recipient.Check = _localNow.ToString();
        recipient.CheckStatus = result.Status;
    }
}
