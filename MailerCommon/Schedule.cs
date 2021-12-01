namespace MailerCommon;

public class Assignment
{
    public readonly static Assignment Empty = new EmptyAssignment();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } 
    public int School { get; set; }
    public Friend Friend { get; set; } = Friend.Nobody;
    public string Meeting { get; set; } = string.Empty;
    public string MeetingName { get; set; } = string.Empty;
}

public class EmptyAssignment : Assignment
{
    public EmptyAssignment()
    {
        Name = "Unassigned";
    }
}

public class Meeting
{
    public readonly static Meeting Empty = new EmptyMeeting();
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Dictionary<string, Assignment> Assignments { get; set; } = new Dictionary<string, Assignment>();
}

public class EmptyMeeting : Meeting
{
    public EmptyMeeting()
    {
        Name = "Empty";
    }
}