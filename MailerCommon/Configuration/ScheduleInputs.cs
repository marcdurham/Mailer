namespace MailerCommon.Configuration
{
    public class ScheduleInputs
    {
        public string? EmailRecipientsDocumentId { get; set; }
        public string? EmailRecipientsRange { get; set; }
        public string? AssignmentListDocumentId { get; set; }
        public string? MeetingName { get; set; }
        public string? HtmlTemplatePath { get; set; }
        public string? AssignmentListRange { get; set; }
        public DayOfWeek SendDayOfWeek { get; set; }
        public DayOfWeek MeetingDayOfWeek { get; set; }
    }
}
