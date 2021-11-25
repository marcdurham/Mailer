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
            var meeting = latest[wk].Midweek; // different

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

        Console.WriteLine("");
        Console.WriteLine($"Thing {friendName}"); // different
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
        }

        var builder = new StringBuilder(1000);
        builder.AppendLine($"<div><h3>Hello {friendName}, you have {futurePresentDays.Count} upcoming CLM assignments</h3><ul>");
        builder.Append(lineBuilder.ToString());
        builder.AppendLine("</ul></div>");

        template = template.Replace("@{UpcomingAssignmentsList}", builder.ToString());

        return template;
    }

}
