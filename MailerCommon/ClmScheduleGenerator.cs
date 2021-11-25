using GoogleAdapter.Adapters;
using System.Text;

namespace MailerCommon;
public class ClmScheduleGenerator
{
    public static string Generate(
        string friendName,
        string template,
        Dictionary<string, Friend> friendMap,
        Schedule schedule)
    {
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");
        Console.WriteLine("This Month (New)");
        var latest = schedule.Weeks.Where(w => w.Start >= thisMonday).OrderBy(w => w.Start).Take(4).ToList();
        for (int wk = 0; wk < 4; wk++)
        {
            string weekKey = latest[wk].Start.ToString("yyyy-MM-dd");
            var meeting = latest[wk].Midweek;

            template = template.Replace($"@{{Day{wk}}}", meeting.Date.ToString("yyyy-MM-dd"));
            foreach(Assignment assignment in meeting.Assignments.Select(a => a.Value).ToList())
            {
                string htmlName = "Friend";
                htmlName = $"{assignment.Friend.PinYinName}<br/>{assignment.Friend.SimplifiedChineseName}<br/>{assignment.Friend.Name}";

                if (string.Equals(friendName, assignment.Friend.Name, StringComparison.OrdinalIgnoreCase))
                {
                    htmlName = $"<span class='selected-friend'>{htmlName}</span>";
                }

                Console.WriteLine($"{weekKey}:{assignment.Name}:{assignment.Friend.Name}");
                template = template.Replace($"@{{{assignment.Name}:{wk}}}", htmlName);
            }
        }

        //Console.WriteLine("This Month (Old)");
        //for (int weekColumnIndex = 0; weekColumnIndex < 4; weekColumnIndex++)
        //{
        //    DateTime day = thisMonday.AddDays(7 * weekColumnIndex); // Monday
        //    DateTime dayOfClm = day.AddDays(3); // Thursday
        //    string weekKey = day.ToString("yyyy-MM-dd");
        //    Console.WriteLine($"Day: {weekKey}");

        //    template = template.Replace($"@{{Day{weekColumnIndex}}}", dayOfClm.ToString("yyyy-MM-dd"));
            
        //    for (int assignmentColumnIndex = 0; assignmentColumnIndex < schedule.Days[weekKey].Length; assignmentColumnIndex++)
        //    {
        //        string name = schedule.Days[weekKey][assignmentColumnIndex].ToString();
        //        string htmlName = name;
        //        if (friendMap.ContainsKey(name.ToUpperInvariant()))
        //        {
        //            Friend f = friendMap[name.ToUpperInvariant()];
        //            htmlName = $"{f.PinYinName}<br/>{f.SimplifiedChineseName}<br/>{f.Name}";
        //        }

        //        if (string.Equals(friendName, name, StringComparison.OrdinalIgnoreCase))
        //        {
        //            htmlName = $"<span class='selected-friend'>{htmlName}</span>";
        //        }

        //        Console.WriteLine($"{weekKey}:{schedule.Headers[assignmentColumnIndex]}:{name}");
        //        template = template.Replace($"@{{{schedule.Headers[assignmentColumnIndex]}:{weekColumnIndex}}}", htmlName);
        //    }
        //}

        Console.WriteLine("");
        Console.WriteLine($"Thing {friendName}");
        //var futurePresentDays = schedule.Weeks.Select(w => w.Midweek).Select(m => m.Assignments) .Days.Keys.Where(k => DateTime.Parse(k.ToString()) >= thisMonday).ToList();
        var futurePresentDays = schedule.AllAssignments()
            .Where(a => a.Date >= thisMonday)
            .Where(a => a.Friend.Name.Equals(friendName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Date)
            .ToList();

        var lineBuilder = new StringBuilder(5000);

        int count = 0;
        foreach (Assignment assignment in futurePresentDays)
        {
            lineBuilder.AppendLine($"<li>{assignment.Date.ToString("yyyy MMM-dd dddd")}: {assignment.Name}</li>");
            //var parsedDay = DateTime.Parse(day);
            //string longDay = parsedDay.AddDays(3).ToString("yyyy MMM-dd dddd"); // Thursday
            //for (int p = 0; p < schedule.Days[day].Length; p++)
            //{
            //    if (string.Equals(schedule.Days[day][p], friendName, StringComparison.OrdinalIgnoreCase))
            //    {
            //        count++;
            //        lineBuilder.AppendLine($"<li>{longDay}: {schedule.Headers[p]}</li>");
            //        Console.WriteLine($"{longDay}:{schedule.Headers[p]}");
            //    }
            //}
        }

        var builder = new StringBuilder(1000);
        builder.AppendLine($"<div><h3>Hello {friendName}, you have {futurePresentDays.Count} upcoming CLM assignments</h3><ul>");
        builder.Append(lineBuilder.ToString());
        builder.AppendLine("</ul></div>");

        template = template.Replace("@{UpcomingAssignmentsList}", builder.ToString());

        return template;
    }

    public static Schedule GetSchedule(IList<IList<object>> values, Dictionary<string, Friend> friendMap)
    {
        const int WeekKeyColumnIndex = 0;
        const int HeaderRowIndex = 0;
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));

        string[] headers = new string[values[HeaderRowIndex].Count];
        var assignmentNames = new Dictionary<string, string>();
        for (int col = 0; col < values[HeaderRowIndex].Count; col++)
        {
            string assignmentName = values[HeaderRowIndex][col]?.ToString() ?? string.Empty;
            headers[col] = assignmentName;
            assignmentNames[assignmentName.ToUpper()] = assignmentName;
        }

        var schedule = new Schedule()
        {
            NextMeetingDate = thisMonday.AddDays(3),
            //Headers = headers, // TODO: Find out if this is needed
        };

        string[] rows = new string[values.Count];
        for (int wk = 1; wk < values.Count; wk++)
        {
            rows[wk] = values[wk][WeekKeyColumnIndex]?.ToString() ?? string.Empty;
            var monday = DateTime.Parse(values[wk][WeekKeyColumnIndex].ToString() ?? string.Empty);
            var clmMeeting = new Meeting 
            { 
                Name = "CLM", 
                Date = monday.AddDays(3) 
            };

            var week = new ScheduleWeek
            {
                Start = monday,
                Midweek = clmMeeting
            };

            for(int a = 2; a < values[wk].Count; a++)
            {
                string assigneeName = values[wk][a]?.ToString() ?? string.Empty;
                Friend assignee;
                if (friendMap.ContainsKey(assigneeName.ToUpperInvariant()))
                {
                    assignee = friendMap[assigneeName.ToUpperInvariant()];
                }
                else
                {
                    assignee = new MissingFriend(assigneeName);
                }

                string assignementKey = headers[a];
                var assignment = new Assignment
                {
                    Key = assignementKey,
                    Name = assignmentNames[assignementKey.ToUpper()],
                    Date = clmMeeting.Date,
                    School = 0,
                    Friend = assignee,
                };

                week.Midweek.Assignments[assignment.Key] = assignment;
            }

            schedule.Weeks.Add(week);
        }

        //Dictionary<string, string[]> days = new();
        //for (int i = 1; i < values.Count; i++)
        //{
        //    string[] week = new string[values[i].Count];
        //    for (int j = 0; j < values[i].Count; j++)
        //    {
        //        week[j] = values[i][j]?.ToString() ?? string.Empty;
        //    }
        //    days[week[0]] = week;
        //}

        return schedule;
    }

    public static Dictionary<string, Friend> GetFriends(Sheets sheets, string documentId)
    {
        IList<IList<object>> friendInfoRows = sheets.Read(documentId: documentId, range: "Friend Info!B1:AI500");

        var friendMap = new Dictionary<string, Friend>();
        foreach (var r in friendInfoRows)
        {
            var friend = new Friend
            {
                Key = r[0].ToString().ToUpperInvariant(),
                Name = r[0].ToString(),
                PinYinName = r[5].ToString(),
                SimplifiedChineseName = r[4].ToString(),
            };
            friendMap[friend.Key] = friend;
            //friendMap[r[0].ToString().ToUpperInvariant()] = $"{r[5]}<br/>{r[4]}<br/>{r[0]}";
        }

        return friendMap;
    }
}
