namespace MailerCommon;
public class Schedule
{
    public DateTime NextMeetingDate { get; set; }
    public List<ScheduleWeek> Weeks { get; set; } = new List<ScheduleWeek>();
    public List<Meeting> AllMeetings()
    {
        var list = new List<Meeting>();
        foreach(var week in Weeks)
        {
            list.AddRange(week.Meetings);
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
    public List<Meeting> Meetings { get; set; } = new List<Meeting>();
}

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