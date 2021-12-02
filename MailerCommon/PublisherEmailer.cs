using GoogleAdapter.Adapters;
using MailerCommon;

namespace Mailer.Sender;

public class PublisherEmailer 
{
    private const string IsoDateFormat = "yyyy-MM-dd";
    readonly IEmailSender _emailSender;
    private readonly ISheets _sheets;

    public PublisherEmailer(ISheets sheets, string? sendGridApiKey, bool dryRunMode =  false, bool forceSendAll = false)
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
        
        ForceSendAll = forceSendAll;
    }

    public bool ForceSendAll { get; set; }

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

        var schedules = new List<ScheduleInputs>()
        {
            new ScheduleInputs()
            {
                MeetingName = "CLM",
                HtmlTemplatePath = "./template1.html",
                EmailRecipientsDocumentId = clmSendEmailsDocumentId,
                EmailRecipientsRange = "CLM Send Emails!B2:F300",
                AssignmentListDocumentId = clmAssignmentListDocumentId,
                AssignmentListRange =  $"CLM Assignment List!B1:AY9999",
                SendDayOfWeek = DayOfWeek.Monday,
                MeetingDayOfWeek = DayOfWeek.Thursday,
            },
            new ScheduleInputs()
            {
                MeetingName = "PW",
                HtmlTemplatePath = "./template3.html",
                EmailRecipientsDocumentId = pwSendEmailsDocumentId,
                EmailRecipientsRange = "PW Send Emails!B2:F300",
                AssignmentListDocumentId = pwAssignmentListDocumentId,
                AssignmentListRange =  $"PW Assignment List!B1:AY9999",
                SendDayOfWeek = DayOfWeek.Wednesday,
                MeetingDayOfWeek = DayOfWeek.Saturday
            },
            new ScheduleInputs()
            {
                MeetingName = "MFS",
                HtmlTemplatePath = "./template4.html",
                EmailRecipientsDocumentId = mfsSendEmailsDocumentId,
                EmailRecipientsRange = "Service Send Emails!B2:F300",
                AssignmentListDocumentId = mfsAssignmentListDocumentId,
                AssignmentListRange =  $"Service Schedule!B1:AY9999",
                SendDayOfWeek = DayOfWeek.Sunday,
                MeetingDayOfWeek = (DayOfWeek)0,
            }
        };

        Console.WriteLine();
        Console.WriteLine("Loading Friends...");
        IList<IList<object>> friendInfoRows = _sheets.Read(documentId: friendInfoDocumentId, range: "Friend Info!B1:Z500");
        var friendMap = FriendLoader.GetFriends(friendInfoRows);
        Console.WriteLine($"{friendMap.Count} Friends Loaded");

        Console.WriteLine();
        Console.WriteLine("Sending schedules...");
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
       
        foreach(ScheduleInputs schedule in schedules)
        {
            SendSchedulesFor(schedule, friendMap, thisMonday);
        }

        Console.WriteLine();
        Console.WriteLine("Done");
    }

    void SendSchedulesFor(ScheduleInputs scheduleInputs, Dictionary<string, Friend> friendMap, DateTime thisMonday)
    {
        string htmlTemplate = File.ReadAllText(scheduleInputs.HtmlTemplatePath);

        Console.WriteLine();
        Console.WriteLine($"Loading {scheduleInputs.MeetingName} Email Recipients...");
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

        Console.WriteLine();
        Console.WriteLine($"Writing status of {scheduleInputs.MeetingName} emails to recipients...");
        foreach (EmailRecipient publisher in recipients)
        {
            sendEmailsRows[recipients.IndexOf(publisher)] = new object[4] {
                publisher.Name,
                publisher.EmailAddress,
                publisher.Sent,
                "Preparing to send email" };
        }

        _sheets.Write(
            documentId: scheduleInputs.EmailRecipientsDocumentId,
            range: scheduleInputs.EmailRecipientsRange,
            values: sendEmailsRows);

        Console.WriteLine();
        Console.WriteLine($"Loading Assignment List for {scheduleInputs.MeetingName}...");
        IList<IList<object>> values = _sheets.Read(documentId: scheduleInputs.AssignmentListDocumentId, range: scheduleInputs.AssignmentListRange);
        List<Meeting> meetings = ScheduleLoader.GetSchedule(values, friendMap, new int[] { (int)scheduleInputs.MeetingDayOfWeek }, scheduleInputs.MeetingName);


        Console.WriteLine();
        Console.WriteLine($"Generating HTML {scheduleInputs.MeetingName} schedules and sending {scheduleInputs.MeetingName} emails...");
        List<Meeting> allMeetings = meetings
            .Where(m => m.Date >= thisMonday && m.Date <= thisMonday.AddDays(35) && m.Name == scheduleInputs.MeetingName)
            .OrderBy(m => m.Date)
            .ToList();

        string html = HtmlScheduleGenerator.Generate(
            html: htmlTemplate,
            meetings: allMeetings);

        Console.WriteLine();
        Console.WriteLine($"Sending {scheduleInputs.MeetingName} schedules and setting status...");
        foreach (EmailRecipient recipient in recipients)
            GenerateAndSendEmailFor(html, allMeetings, recipient, scheduleInputs.SendDayOfWeek);

        Console.WriteLine();
        Console.WriteLine($"Writing status of emails to {scheduleInputs.MeetingName} recipients...");
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
        EmailRecipient recipient,
        DayOfWeek sendDayOfWeek)
    {
        htmlMessageText = HtmlScheduleGenerator.Highlight(recipient.Friend, htmlMessageText);

        htmlMessageText = HtmlScheduleGenerator.InjectUpcomingAssignments(
            friendName: recipient.Name,
            friend: recipient.Friend,
            template: htmlMessageText,
            meetings: meetings);

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

        if(!ForceSendAll && (sent.AddDays(7) >= DateTime.Today 
            && DateTime.Today.DayOfWeek == sendDayOfWeek
            || sent.AddDays(8) >= DateTime.Today))
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
}
