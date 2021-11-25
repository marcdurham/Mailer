using GoogleAdapter.Adapters;
using System.Text;

namespace MailerCommon;
public class ClmScheduleGenerator
{
    public static string Generate(
        Sheets sheets,
        string googleApiSecretsJson, 
        string documentId, 
        string range, 
        string friendName,
        string template,
        Dictionary<string, string> friendMap,
        Schedule schedule)
    {
            ////string template = File.ReadAllText("./template1.html");

        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");
        Console.WriteLine("This Month");
        for (int weekColumnIndex = 0; weekColumnIndex < 4; weekColumnIndex++)
        {
            DateTime day = thisMonday.AddDays(7 * weekColumnIndex);
            DateTime dayOfClm = day.AddDays(3);
            string dayKey = day.ToString("yyyy-MM-dd");
            Console.WriteLine($"Day: {dayKey}");

            template = template.Replace($"@{{Day{weekColumnIndex}}}", dayOfClm.ToString("yyyy-MM-dd"));

            for (int assignmentColumnIndex = 0; assignmentColumnIndex < schedule.Days[dayKey].Length; assignmentColumnIndex++)
            {
                string name = schedule.Days[dayKey][assignmentColumnIndex].ToString();
                string allNames = friendMap.ContainsKey(name.ToUpperInvariant()) ? friendMap[name.ToUpperInvariant()] : name;
                string htmlName = allNames;
                string flag = string.Empty;
                if (string.Equals(friendName, name, StringComparison.OrdinalIgnoreCase))
                {
                    htmlName = $"<span class='selected-friend'>{allNames}</span>";
                    flag = "***";
                }

                Console.WriteLine($"{dayKey}:{schedule.Headers[assignmentColumnIndex]}:{name}{flag}");
                template = template.Replace($"@{{{schedule.Headers[assignmentColumnIndex]}:{weekColumnIndex}}}", htmlName);
            }
        }

        Console.WriteLine("");
        Console.WriteLine($"Thing {friendName}");
        var futurePresentDays = schedule.Days.Keys.Where(k => DateTime.Parse(k.ToString()) >= thisMonday).ToList();
        var lineBuilder = new StringBuilder(1000);

        int count = 0;
        foreach (var day in futurePresentDays)
        {
            var parsedDay = DateTime.Parse(day);
            string longDay = parsedDay.AddDays(3).ToString("yyyy MMM-dd dddd"); // Thursday
            for (int p = 0; p < schedule.Days[day].Length; p++)
            {
                if (string.Equals(schedule.Days[day][p], friendName, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    lineBuilder.AppendLine($"<li>{longDay}: {schedule.Headers[p]}</li>");
                    Console.WriteLine($"{longDay}:{schedule.Headers[p]}");
                }
            }
        }

        var builder = new StringBuilder(1000);
        builder.AppendLine($"<div><h3>Hello {friendName}, you have {count} upcoming CLM assignments</h3><ul>");
        builder.Append(lineBuilder.ToString());
        builder.AppendLine("</ul></div>");

        //if(count > 0)
        template = template.Replace("@{UpcomingAssignmentsList}", builder.ToString());

        //File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", template);
        return template;
    }

    public static Schedule GetSchedule(Sheets sheets, string documentId)
    {
        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        IList<IList<object>> values = sheets.Read(documentId: documentId, range: "CLM Assignment List!B1:AY9999");

        string[] headers = new string[values[0].Count];
        for (int i = 0; i < values[0].Count; i++)
        {
            headers[i] = values[0][i].ToString();
        }

        string[] rows = new string[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            rows[i] = values[i][0].ToString();
        }

        Dictionary<string, string[]> schedule = new();
        for (int i = 1; i < values.Count; i++)
        {
            string[] week = new string[values[i].Count];
            for (int j = 0; j < values[i].Count; j++)
            {
                week[j] = values[i][j].ToString();
                //Console.WriteLine($"{rows[i]}:{headers[j]}:{values[i][j]}");
            }
            schedule[week[0]] = week;
            //Console.WriteLine();
        }

        return new Schedule
        {
            NextMeetingDate = thisMonday.AddDays(3),
            Headers = headers,
            Days = schedule,
        };
    }

    public static Dictionary<string, string> GetFriends(Sheets sheets, string documentId)
    {
        IList<IList<object>> friendInfoRows = sheets.Read(documentId: documentId, range: "Friend Info!B1:AI500");

        var friendList = new List<string>();
        var friendMap = new Dictionary<string, string>();
        foreach (var r in friendInfoRows)
        {
            friendList.Add(r[0].ToString());
            friendMap[r[0].ToString().ToUpperInvariant()] = $"{r[5]}<br/>{r[4]}<br/>{r[0]}";
        }

        Console.WriteLine();
        Console.WriteLine("Friends:");
        foreach (string friend in friendList)
        {
            Console.WriteLine($"{friend}: {friendMap[friend.ToUpperInvariant()]}");
        }

        return friendMap;
    }
}
