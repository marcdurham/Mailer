using System.Text;
using System.Text.RegularExpressions;

namespace MailerCommon;
public class HtmlScheduleGenerator
{
    public static string Generate(string html, List<Meeting> meetings)
    {
        foreach (Meeting m in meetings)
        {
            int wk = meetings.IndexOf(m);
            html = html.Replace($"@{{Day{wk}}}", meetings[wk].Date.ToString("yyyy-MM-dd"));
            html = html.Replace($"@{{DayOfWeek{wk}}}", meetings[wk].Date.ToString("dddd"));
        }

        Regex regex = new(@"@{.+}");

        MatchCollection? matches = regex.Matches(html);

        foreach (Match match in matches)
        {
            string value = match.Value;
            string replacement = string.Empty;
            Match m = Regex.Match(value, @"@{([^:]+)(:(\d+))?(:([^:0-9]+))?}");
            if (m.Success)
            {
                string key = m.Groups[1].Value;
                string index = m.Groups[3].Value;
                string property = m.Groups[5].Value;
                
                replacement = ParseAndReplace(meetings, value, key, index, property);
            }

            string escaped = value.Replace("(", "\\(").Replace(")", "\\)");
            string pattern = $"<td(\\s+class=\")?([a-zA-Z0-9-]+)?(\")?>({escaped})</td>";

            html = Regex.Replace(html, pattern, replacement);
        }

        return html;
    }

    private static string ParseAndReplace(List<Meeting> meetings, string value, string key, string index, string property)
    {
        try
        {
            string replacement = string.Empty;
            string friendKey = string.Empty;
            int.TryParse(index, out int indexValue);

            if (meetings[indexValue].Assignments.ContainsKey(key))
            {
                friendKey = meetings[indexValue].Assignments[key].Friend.Key;

                replacement = GetReplacementFor(
                    meetings[indexValue].Assignments[key],
                    property);
            }

            return $"<td$1$2$3 data-friend-key='{friendKey}'>{replacement}</td>";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting replacement for data-friend-key: at value: {value} key: {key} index: {index} property: {property}", ex);
        }
    }

    private static string GetReplacementFor(Assignment assignment, string property)
    {
        string replacement;

        try
        {
            if (string.Equals(property, "N") || string.Equals(property, "Name"))
            {
                replacement = $"{assignment.Name}";
            }
            else if (string.Equals(property, "E"))
            {
                replacement = $"{assignment.Friend.EnglishName}";
            }
            else if (string.Equals(property, "CHS"))
            {
                replacement = $"{assignment.Friend.SimplifiedChineseName}";
            }
            else if (string.Equals(property, "E/PY/CHS"))
            {
                replacement = $"{assignment.Friend.PinYinName}<br/>{ assignment.Friend.SimplifiedChineseName}<br/>{ assignment.Friend.Name}";
            }
            else if (string.Equals(property, "CHS/E"))
            {
                replacement = $"{assignment.Friend.SimplifiedChineseName} {assignment.Friend.Name}";
            }
            else if (string.Equals(property, "NoService"))
            {
                replacement = assignment.Friend.Name;
                if (replacement.Contains("/"))
                {
                    replacement = assignment.Friend.Name.Split("/").Last();
                }
            }
            else
            {
                replacement = $"{assignment.Friend.PinYinName}<br/>{ assignment.Friend.SimplifiedChineseName}<br/>{ assignment.Friend.Name}";
            }
        }
        catch (Exception ex)
        {
            throw new Exception(
                message: $"Error finding replacement for property: {property} with assignment: {assignment.Name} with friend name: {(assignment.Friend?.Name ?? "NULL")}", 
                innerException: ex);
        }

        return replacement;
    }

    public static (string html, List<Assignment> friendAssignments) InjectUpcomingAssignments(
        string friendName, 
        Friend friend, 
        string template, 
        IEnumerable<Meeting> meetings,
        IEnumerable<Meeting> allMeetings)
    {
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");

        var assignments = new List<Assignment>();
        foreach (Meeting meeting in meetings)
            assignments.AddRange(meeting.Assignments.Values.ToList());

        var futurePresentDays = assignments
            .Where(a => a.Date >= thisMonday)
            .Where(a => 
                a.Friend.Name.Equals(friendName, StringComparison.OrdinalIgnoreCase)
                || a.Friend.SimplifiedChineseName.Equals(friendName, StringComparison.OrdinalIgnoreCase)
                || a.Friend.PinYinName.Equals(friendName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Date)
            .ToList();

        var lineBuilder = new StringBuilder(5000);

        foreach (Assignment assignment in futurePresentDays)
        {
            lineBuilder.AppendLine($"<li>{assignment.Date.ToString("yyyy MMM-dd dddd")}: <strong>{assignment.MeetingTitle}:</strong> {assignment.Name}</li>");
        }

        var upcomingAssignments = new StringBuilder(1000);
        upcomingAssignments.AppendLine($"<div><h3>Hello {friendName}, you have {futurePresentDays.Count} upcoming assignments</h3><ul>");
        upcomingAssignments.Append(lineBuilder.ToString());
        upcomingAssignments.AppendLine("</ul></div>");

        template = Regex.Replace(template, @"<\s*inject-upcoming-assignments-here\s*/\s*>", upcomingAssignments.ToString(), RegexOptions.IgnoreCase);

        var allAssignments = new List<Assignment>();
        foreach (Meeting meeting in allMeetings)
            allAssignments.AddRange(meeting.Assignments.Values.ToList());
            
        var allFriendAssignments = allAssignments
            .Where(a => 
                a.Friend.Name.Equals(friendName, StringComparison.OrdinalIgnoreCase)
                || a.Friend.SimplifiedChineseName.Equals(friendName, StringComparison.OrdinalIgnoreCase)
                || a.Friend.PinYinName.Equals(friendName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Date)
            .ToList();

        return (template, allFriendAssignments);
    }

    public static string Highlight(Friend friend, string html)
    {
        if (!string.IsNullOrWhiteSpace(friend.Key))
            html = Regex.Replace(
                html,
                @"<td\s+((class=\"")([^\""]+)\"")?(\s*data-friend-key='" + friend.Key + "')>(.*)</td>",
                "<td class=\"$3 selected-assignee\" $4>$5</td>");

        return html;
    }
}
