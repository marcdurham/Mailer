// See https://aka.ms/new-console-template for more information
using GoogleAdapter.Adapters;
using System.Text;

Console.WriteLine("Mailer");

string secretsJsonPath = args[0];
string documentId = args[1];
string range = args[2];
string friendName = args[3];

string json = File.ReadAllText(secretsJsonPath);

var sheets = new Sheets(json, isServiceAccount: true);

IList<IList<object>> values = sheets.Read(documentId: documentId, range: range);

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
    for(int j = 0; j < values[i].Count; j++)
    {
        week[j] = values[i][j].ToString();
        //Console.WriteLine($"{rows[i]}:{headers[j]}:{values[i][j]}");
    }
    schedule[week[0]] = week;
    //Console.WriteLine();
}

StringBuilder builder = new (10_000);
builder.AppendLine("<html><head></head><body>");



string today = DateTime.Today.ToString("yyyy-MM-dd");
Console.WriteLine("This Month");
for (DateTime day = DateTime.Today; day <= DateTime.Today.AddDays(7 * 4); day = day.AddDays(7))
{
    string dayKey = day.ToString("yyyy-MM-dd");
    builder.AppendLine($"<div><h2>{dayKey}</h2>");

    Console.WriteLine($"Day: {dayKey}");
    for (int j = 0; j < schedule[dayKey].Length; j++)
    {
        string name = schedule[dayKey][j].ToString();
        string flag = string.Empty;
        if (string.Equals(friendName, name, StringComparison.OrdinalIgnoreCase))
            flag = "***";

        Console.WriteLine($"{dayKey}:{headers[j]}:{schedule[dayKey][j]}{flag}");
        builder.AppendLine($"<div><h3>{headers[j]}</h3><span>{schedule[dayKey][j]}{flag}</span></div>");
    }

    builder.AppendLine($"</div>");
}

Console.WriteLine("");
Console.WriteLine($"Thing {friendName}");
var futurePresentDays = schedule.Keys.Where(k => DateTime.Parse(k.ToString()) >= DateTime.Today).ToList();
foreach(var day in futurePresentDays)
{
    for(int p = 0; p < schedule[day].Length; p++)
    {
        if(string.Equals(schedule[day][p], friendName, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"{day}:{headers[p]}");
        }
    }
}

builder.AppendLine("</body></html>");

File.WriteAllText(@"c:\Users\Marc\Desktop\template5.html", builder.ToString());