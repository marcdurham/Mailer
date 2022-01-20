namespace MailerCommon;

public class Assignment
{
    public readonly static Assignment Empty = new EmptyAssignment();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeOnly Start { get; set; } = TimeOnly.MinValue;
    public int School { get; set; }
    public Friend Friend { get; set; } = Friend.Nobody;
    public string Meeting { get; set; } = string.Empty;
    public string MeetingTitle { get; set; } = string.Empty;
}

public class EmptyAssignment : Assignment
{
    public EmptyAssignment()
    {
        Name = "Unassigned";
    }
}