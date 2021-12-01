namespace MailerCommon
{
    public class ScheduleInputs
    {
        public string SendEmailsDocumentId { get; set; }
        public string SendEmailsRange { get; set;  }
        public DayOfWeek SendDayOfWeek { get; internal set; }
        public string AssignmentListDocumentId { get; internal set; }
        public string MeetingName { get; internal set; }
        public string HtmlTemplate { get; internal set; }
    }
}
