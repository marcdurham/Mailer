namespace MailerCommon.Configuration
{
    public class ConfigurationFile
    {
        public EmailSenderConfiguration[] EmailSenders { get; set; } = new EmailSenderConfiguration[] { };
        public bool DryMode { get; set; }
        public bool ForceSend { get; set; }
        public ScheduleInputs[] Schedules { get; set; } = new ScheduleInputs[] { };
    }
}
