namespace MailerCommon
{
    public class ScheduleInputs
    {
        public string EmailRecipientsDocumentId { get; set; }
        public string EmailRecipientsRange { get; set;  }
        public DayOfWeek SendDayOfWeek { get; internal set; }
        public string AssignmentListDocumentId { get; internal set; }
        public string MeetingName { get; internal set; }
        public string HtmlTemplatePath { get; internal set; }
        public string AssignmentListRange { get; internal set; }
        public DayOfWeek MeetingDayOfWeek { get; internal set; }
    }
}
