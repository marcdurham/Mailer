﻿namespace MailerCommon.Configuration
{
    public class ScheduleOptions
    {
        public string? TimeZone { get; set; }
        public double? TimeZoneOffsetHours { get; set; } = 0.0;
        public ScheduleInputs[] Schedules { get; set; }
    }
}
