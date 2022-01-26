namespace MailerCommon;

public class Meeting
{
    public readonly static Meeting Empty = new EmptyMeeting();
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
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