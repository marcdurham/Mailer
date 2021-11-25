using GoogleAdapter.Adapters;
using System.Text;

namespace MailerCommon;
public class ClmScheduleGenerator
{
    public static string Generate(
        //Sheets sheets,
        //string googleApiSecretsJson, 
        //string documentId, 
        //string range, 
        string friendName,
        string template,
        Dictionary<string, Friend> friendMap,
        Schedule schedule)
    {
            ////string template = File.ReadAllText("./template1.html");

        DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
        string today = thisMonday.ToString("yyyy-MM-dd");
        Console.WriteLine("This Month");
        for (int weekColumnIndex = 0; weekColumnIndex < 4; weekColumnIndex++)
        {
            DateTime day = thisMonday.AddDays(7 * weekColumnIndex); // Monday
            DateTime dayOfClm = day.AddDays(3); // Thursday
            string weekKey = day.ToString("yyyy-MM-dd");
            Console.WriteLine($"Day: {weekKey}");

            template = template.Replace($"@{{Day{weekColumnIndex}}}", dayOfClm.ToString("yyyy-MM-dd"));

            for (int assignmentColumnIndex = 0; assignmentColumnIndex < schedule.Days[weekKey].Length; assignmentColumnIndex++)
            {
                string name = schedule.Days[weekKey][assignmentColumnIndex].ToString();
                string htmlName = name;
                if (friendMap.ContainsKey(name.ToUpperInvariant()))
                {
                    Friend f = friendMap[name.ToUpperInvariant()];
                    htmlName = $"{f.PinYinName}<br/>{f.SimplifiedChineseName}<br/>{f.Name}";
                }

                if (string.Equals(friendName, name, StringComparison.OrdinalIgnoreCase))
                {
                    htmlName = $"<span class='selected-friend'>{htmlName}</span>";
                }

                Console.WriteLine($"{weekKey}:{schedule.Headers[assignmentColumnIndex]}:{name}");
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
