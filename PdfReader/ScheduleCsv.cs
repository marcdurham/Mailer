using CsvHelper;
using System.Globalization;

namespace PdfReader;

public class ScheduleCsv
{
    public void Convert(Schedule schedule, string path)
    {
        //TextWriter textWriter = new();
        //CsvWriter writer = new(TextWriter, configuration)


        using (var writer = new StreamWriter(path))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            List<Assignment> assignments = schedule.Meetings
                .First()
                .Assignments;

            csv.WriteRecords(assignments);
        }
    }
}
