namespace Mailer;

public class CalendarOptions 
{
    public Calendar[]? Calendars { get; set; }
}

public class Calendar
{
    public string? Name { get; set; }
    public string? IcsUri { get; set; }
}
