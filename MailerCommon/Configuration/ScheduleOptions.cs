namespace MailerCommon.Configuration
{
    public class ScheduleOptions
    {
        public ScheduleInputs[] Schedules { get; set; }
        public string? EmailFromName { get; set; }
        public string? EmailFromAddress { get; set; }
    }
}
