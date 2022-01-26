using GoogleAdapter.Adapters;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using MailerCommon;
using MailerCommon.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    const string IsoDateFormat = "yyyy-MM-dd";
    
    readonly IEmailSender _emailSender;
    readonly ScheduleOptions _options;
    readonly ICustomLogger<PublisherEmailer> _logger;
    readonly IMemoryCache _memoryCache;
    readonly ISheets _sheets;

    public PublisherEmailer(
        ScheduleOptions options,
        ICustomLogger<PublisherEmailer> logger, 
        IMemoryCache memoryCache, 
        ISheets sheets, 
        string? sendGridApiKey, 
        bool dryRunMode =  false, 
        bool forceSendAll = false)
    {
        _options = options;
        _logger = logger;
        _memoryCache = memoryCache;
        _sheets = sheets;

        if (sendGridApiKey == null)
            throw new ArgumentNullException(nameof(sendGridApiKey));

        _emailSender = new EmailSenderProxy(
            new List<IEmailSender>
            {
                new SaveEmailToFileEmailSender() { SendByDefault = dryRunMode },
                new SmtpEmailSender(isSender: m => m.ToAddress.ToUpper().EndsWith("@GMAIL.COM")),
                new SendGridEmailSender(sendGridApiKey, _options) { SendByDefault = true },
            });
        
        ForceSendAll = forceSendAll;
    }

    public bool ForceSendAll { get; set; }

    public void Run(
        string? friendInfoDocumentId,
        List<ScheduleInputs> schedules)
    {
        if (friendInfoDocumentId == null)
            throw new ArgumentNullException(nameof(friendInfoDocumentId));

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

        _logger.LogInformation("Done");
    }

    void SendSchedulesFor(
        ScheduleInputs scheduleInputs, 
        Dictionary<string, Friend> friendMap, 
        DateTime thisMonday)
    {
        string htmlTemplate = File.ReadAllText(scheduleInputs.HtmlTemplatePath);

        _logger.LogInformation($"Loading {scheduleInputs.MeetingName} Email Recipients...");
        IList<IList<object>> sendEmailsRows = _sheets.Read(scheduleInputs.EmailRecipientsDocumentId, scheduleInputs.EmailRecipientsRange);
        List<EmailRecipient> recipients = EmailRecipientLoader.ConvertToEmailRecipients(sendEmailsRows);

        foreach (EmailRecipient recipient in recipients)
        {
            //recipient.HtmlMessage = htmlTemplate;

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

        _logger.LogInformation($"Writing status of {scheduleInputs.MeetingName} emails to recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            sendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                $"{DateTime.Now}: Preparing to send email" };
        }

        _sheets.Write(
            documentId: scheduleInputs.EmailRecipientsDocumentId,
            range: scheduleInputs.EmailRecipientsRange,
            values: sendEmailsRows);

        _logger.LogInformation($"Loading Assignment List for {scheduleInputs.MeetingName}...");
        IList<IList<object>> values = _sheets.Read(documentId: scheduleInputs.AssignmentListDocumentId, range: scheduleInputs.AssignmentListRange);
        List<Meeting> meetings = ScheduleLoader.GetSchedule(
            values, 
            friendMap, 
            new int[] { (int)scheduleInputs.MeetingDayOfWeek }, 
            scheduleInputs.MeetingName);

        _logger.LogInformation($"Generating HTML {scheduleInputs.MeetingName} schedules and sending {scheduleInputs.MeetingName} emails...");
        List<Meeting> upcomingMeetings = meetings
            .Where(m => m.Date >= thisMonday && m.Date <= thisMonday.AddDays(35) && m.Name == scheduleInputs.MeetingName)
            .OrderBy(m => m.Date)
            .ToList();

        string html = HtmlScheduleGenerator.Generate(
            html: htmlTemplate,
            meetings: upcomingMeetings);

        _logger.LogInformation($"Sending {scheduleInputs.MeetingName} schedules and setting status...");
        foreach (EmailRecipient recipient in recipients)
            GenerateAndSendEmailFor(html, upcomingMeetings, meetings, recipient, scheduleInputs);

        _logger.LogInformation($"Writing status of emails to {scheduleInputs.MeetingName} recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            sendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                publisher.Result };
        }

        _sheets.Write(
            documentId: scheduleInputs.EmailRecipientsDocumentId,
            range: scheduleInputs.EmailRecipientsRange,
            values: sendEmailsRows);
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

        CacheFriendAssignments(scheduleInputs.MeetingName, recipient, friendAssignments);

        SendEmailFor(subject, htmlMessageText, recipient, sendDayOfWeek);
    }

    private void CacheFriendAssignments(string meetingName, EmailRecipient recipient, List<Assignment> friendAssignments)
    {
        var shortCalendar = new Ical.Net.Calendar();

        foreach (Assignment assignment in friendAssignments)
        {
            string assignmentName = assignment.Name;
            if(assignmentName.Contains(" - "))
                assignmentName = string.Join("-", assignmentName.Split("-", System.StringSplitOptions.RemoveEmptyEntries).Reverse());
            var calEvent = new CalendarEvent
            {
                Start = new CalDateTime(assignment.Date),
                Summary = $"{assignmentName} ({assignment.Meeting})",
            };

            shortCalendar.Events.Add(calEvent);
        }

        var serializer = new CalendarSerializer();
        var serializedCalendar = serializer.SerializeToString(shortCalendar);

        var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromSeconds(60*60)); // One hour

        _memoryCache.Set($"{meetingName}:{recipient.Name.ToUpper()}", serializedCalendar, cacheEntryOptions);
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
            recipient.Result = $"{DateTime.Now}: Skipped: Sent Too Recently";
            return;
        }

        _logger.LogInformation($"Sending email to {recipient.Name}: {recipient.EmailAddress}: {recipient.Sent}...");
        recipient.Result = $"{DateTime.Now}: Sending";

        EmailMessage message = new()
        {
            FromAddress = _options.EmailFromAddress ?? "auto@mailer.org",
            FromName = _options.EmailFromName ?? "Mailer Information Board",
            ToAddress = recipient.EmailAddress!,
            ToName = recipient.Name,
            Subject = subject,
            Text = htmlMessageText
        };

        var result = _emailSender.Send(message);

        recipient.Sent = result.EmailWasSent ? DateTime.Now.ToString() : null;
        recipient.Result = result.Status;
    }
}
