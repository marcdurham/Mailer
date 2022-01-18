namespace MailerCommon.Configuration
{
    public class ScheduleOptions
    {
        public string? TimeZone { get; set; }
        public ScheduleInputs[] Schedules { get; set; }
    }
}
