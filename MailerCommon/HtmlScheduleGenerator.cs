using System.Text;
using System.Text.RegularExpressions;

namespace MailerCommon;
public class HtmlScheduleGenerator
{
    public static string Generate(string template, List<Meeting> meetings)
    {
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");
        Console.WriteLine("This Month (New)");
        var latestMeetings = meetings
            .Where(m => m.Date >= thisMonday)
            .OrderBy(m => m.Date)
            .Take(4)
            .ToList();
        
        template = InjectAssignmentsReverse(template, latestMeetings);

        return template;
    }

    static string InjectAssignmentsReverse(string html, List<Meeting> meetings)
    {
        for (int wk = 0; wk < 4; wk++)
        {
            html = html.Replace($"@{{Day{wk}}}", meetings[wk].Date.ToString("yyyy-MM-dd"));
        }

        Regex regex = new Regex(@"@{.+}");

        MatchCollection? matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string value = match.Value;
            Console.WriteLine($"{value}");
            Match m = Regex.Match(value, @"@{([^:]+)(:(\d+))?(:([^:0-9]+))?}");
            if (m.Success)
            {
                Console.WriteLine($"Key: {m.Groups[1].Value} Index {m.Groups[3].Value} Property: {m.Groups[5].Value}");
                string key = m.Groups[1].Value;
                string index = m.Groups[3].Value;
                string property = m.Groups[5].Value;

                int.TryParse(index, out int indexValue);

                if (meetings[indexValue].Assignments.ContainsKey(key))
                {
                    if (string.Equals(property, "N") || string.Equals(property, "Name"))
                    {
                        Console.WriteLine($"Name: {meetings[indexValue].Assignments[key].Name}");
                        html = html.Replace(value, meetings[indexValue].Assignments[key].Name);
                    }
                    else if (string.Equals(property, "E"))
                    {
                        Console.WriteLine($"E: {meetings[indexValue].Assignments[key].Friend.EnglishName}");
                        html = html.Replace(value, meetings[indexValue].Assignments[key].Friend.EnglishName);
                    }
                    else if (string.Equals(property, "CHS"))
                    {
                        Console.WriteLine($"CHS: {meetings[indexValue].Assignments[key].Friend.SimplifiedChineseName}");
                        html = html.Replace(value, meetings[indexValue].Assignments[key].Friend.SimplifiedChineseName);
                    }
                    else
                    {
                        Console.WriteLine($"A: {meetings[indexValue].Assignments[key].Friend.AllNames()}");
                        string htmlName = $"{ meetings[indexValue].Assignments[key].Friend.PinYinName}<br/>{ meetings[indexValue].Assignments[key].Friend.SimplifiedChineseName}<br/>{ meetings[indexValue].Assignments[key].Friend.Name}";
                        html = html.Replace(value, htmlName);
                    }
                }
                else
                {
                    html = html.Replace(value, string.Empty);
                }
            }
            else
            {
                Console.WriteLine("Parse failed");
            }
            Console.WriteLine();
        }

        return html;
    }

    static string InjectAssignments(string friendName, string template, List<Meeting> latest)
    {
        for (int wk = 0; wk < 4; wk++)
        {
            string weekKey = latest[wk].Date.ToString("yyyy-MM-dd");
            var meeting = latest[wk];

            template = InjectWeekAssignments(friendName, template, wk, weekKey, meeting);
        }

        return template;
    }

    static string InjectWeekAssignments(string friendName, string template, int wk, string weekKey, Meeting meeting)
    {
        template = template.Replace($"@{{Day{wk}}}", meeting.Date.ToString("yyyy-MM-dd"));
        foreach (Assignment assignment in meeting.Assignments.Select(a => a.Value).ToList())
        {
            template = InjectAssignmees(friendName, template, wk, weekKey, assignment);
        }

        return template;
    }

    static string InjectAssignmees(string friendName, string template, int wk, string weekKey, Assignment assignment)
    {
        if (assignment.Key == "Outgoing Speaker 1")
        {
            Console.WriteLine("pause here");

        }
        string htmlName = "Friend";
        htmlName = $"{assignment.Friend.PinYinName}<br/>{assignment.Friend.SimplifiedChineseName}<br/>{assignment.Friend.Name}";

        if (string.Equals(friendName, assignment.Friend.Name, StringComparison.OrdinalIgnoreCase))
        {
            htmlName = $"<span class='selected-friend'>{htmlName}</span>";
        }

        Console.WriteLine($"{weekKey}:{assignment.Name}:{assignment.Friend.Name}");
        template = template.Replace($"@{{{assignment.Name}:{wk}}}", htmlName);
        return template;
    }

    public static string InjectUpcomingAssignments(string friendName, string template, Schedule schedule)
    {
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");

        Console.WriteLine("");
        Console.WriteLine($"Upcoming Assignments for {friendName}");
        var futurePresentDays = schedule.AllAssignments()
            .Where(a => a.Date >= thisMonday)
            .Where(a => a.Friend.Name.Equals(friendName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Date)
            .ToList();

        var lineBuilder = new StringBuilder(5000);

        foreach (Assignment assignment in futurePresentDays)
        {
            lineBuilder.AppendLine($"<li>{assignment.Date.ToString("yyyy MMM-dd dddd")}: {assignment.Name}</li>");
        }

        var upcomingAssignments = new StringBuilder(1000);
        upcomingAssignments.AppendLine($"<div><h3>Hello {friendName}, you have {futurePresentDays.Count} upcoming assignments</h3><ul>");
        upcomingAssignments.Append(lineBuilder.ToString());
        upcomingAssignments.AppendLine("</ul></div>");

        template = Regex.Replace(template, @"<\s*inject-upcoming-assignments-here\s*/\s*>", upcomingAssignments.ToString(), RegexOptions.IgnoreCase);

        return template;
    }
}
