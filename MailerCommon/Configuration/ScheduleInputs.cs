namespace MailerCommon.Configuration
{
    public class ScheduleInputs
    {
        public string? EmailRecipientsDocumentId { get; set; }
        public string? EmailRecipientsRange { get; set; }
        public string? AssignmentListDocumentId { get; set; }
        public string MeetingName { get; set; } = "MTG";
        public string? MeetingTitle { get; set; }
        public string? HtmlTemplatePath { get; set; }
        public string? AssignmentListRange { get; set; }
        public DayOfWeek SendDayOfWeek { get; set; }
        public DayOfWeek MeetingDayOfWeek { get; set; }
        public DateTime? MeetingStartTime { get; set; }
        public int? MeetingDateColumnIndex { get; set; }
        public int? WeekDateColumnIndex { get; set; }
        public int? AssignmentStartColumnIndex { get; set; }
        public int? AssignmentEndColumnIndex { get; set; }
        public bool? HasMultipleMeetingsPerWeek { get; set; }
        public DayOfWeek? StartDayForWeekWithMultipleMeetings { get; set; } = DayOfWeek.Monday;

    }

    public class ScheduleMailingInputs
    {
        public string? EmailRecipientsDocumentId { get; set; }
        public string? EmailRecipientsRange { get; set; }
        public DayOfWeek SendDayOfWeek { get; set; }
    }
}
