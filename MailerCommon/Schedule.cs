namespace MailerCommon;
public class Schedule
{
    public string[] Headers { get; set; }
    public Dictionary<string, string[]> Days { get; set; } = new Dictionary<string, string[]>();
    public DateTime NextMeetingDate { get; set; }
    public List<ScheduleWeek> Weeks { get; set; } = new List<ScheduleWeek>();
    public List<Meeting> AllMeetings()
    {
        var list = new List<Meeting>();
        foreach(var week in Weeks)
        {
            list.AddRange(week.AllMeetings());
        }

        return list;
    }

    public List<Assignment> AllAssignments()
    {
        var list = new List<Assignment>();
        foreach(var meeting in AllMeetings())
        {
            list.AddRange(meeting.Assignments.Select(a => a.Value).ToList());
        }

        return list;
    }
}

public class ScheduleWeek
{
    public DateTime Start { get; set; }
    public Meeting Midweek { get; set; } = Meeting.Empty;
    public Meeting Weekend { get; set; } = Meeting.Empty;
    public Dictionary<DateTime, Meeting> MeetingsForService { get; set; } = new Dictionary<DateTime, Meeting>();

    public List<Meeting> AllMeetings()
    {
        var list = new List<Meeting>()
        { 
            Midweek,
            Weekend
        };

        list.AddRange(MeetingsForService.Select(m => m.Value).ToList());

        return list;
    }
}

public class Assignment
{
    public readonly static Assignment Empty = new EmptyAssignment();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; } 
    public int School { get; set; }
    public Friend Friend { get; set; } = Friend.Nobody;
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