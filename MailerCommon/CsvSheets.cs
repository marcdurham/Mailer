using CsvHelper;
using CsvHelper.Configuration;
using GoogleAdapter.Adapters;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MailerCommon
{
    public class CsvSheets : ISheets
    {
        public IList<IList<object>> Read(string documentId, string range)
        {
            // Valid example range: "My Sheet!C3:ZZ99"
            Regex regex = new Regex(@"([^!]+)(!(([a-zA-Z]+)(\d+):([a-zA-Z]+)(\d+)))");
            Match? match = regex.Match(range);
            if (!match.Success || match.Groups.Count < 8)
                throw new ArgumentException("Invalid range");

            string startColumnString = match.Groups[4].Value.ToUpper();
            int startRow = int.Parse(match.Groups[5].Value) - 1;
            int endRow = int.Parse(match.Groups[7].Value) - 1;

            if (startColumnString.Length != 1)
                throw new ArgumentException($"Starting column {startColumnString} can only be one character wide, A thru Z. AA and higher is not permitted.");

            int startColumn = ColumnIndex(startColumnString);

            string lastColumnString = match.Groups[6].Value.ToUpper();

            if (startColumnString.Length != 1)
                throw new ArgumentException($"Starting column {startColumnString} can only be one character wide, A thru Z. AA and higher is not permitted.");

            int lastColumn = ColumnIndex(lastColumnString);

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
              ShouldSkipRecord = row => false,
              
            };
            configuration.PrepareHeaderForMatch = (h) =>
            { 
                if(string.IsNullOrWhiteSpace(h.Header))
                {
                    // Column headers must be unique, use a Guid to make it unique
                    // Prefix the header with MISSING- so detect it and skip it
                    return $"MISSING-{Guid.NewGuid()}";
                }

                return h.Header;
            };

            using (var reader = new StreamReader(documentId))
            using (var csv = new CsvReader(reader, configuration))
            {
                var output = new List<IList<object>>();
                
                for (int r = 0; r < startRow; r++)
                    csv.Read();

                int rowNumber = startRow;
                csv.Read();
                csv.ReadHeader();
                rowNumber++;

                int headerCount = csv.HeaderRecord.Count();

                var headerList = new List<object>();
                for(int col = startColumn; col <= lastColumn && col < headerCount; col++)
                {
                    string header = csv.HeaderRecord[col];
                    headerList.Add(header);
                }
                
                output.Add(headerList);

                while (csv.Read())
                {
                    if (rowNumber > endRow)
                        break;

                    var row = new List<object>();
                    for(int col = startColumn; col <= lastColumn && col < headerCount; col++)
                    {
                        object val = csv[col];
                        row.Add(csv[col]);
                    }

                    output.Add(row);
                    rowNumber++;
                }

                return output;
            }
        }

        public static int ColumnIndex(string column)
        {
            if (column.Length > 2)
                throw new ArgumentException("Columns cannot be greater than 2 characters wide, ZZ");

            int value = (column!.ToUpper()[column.Length - 1] - 'A');
            for(int c = (column.Length-2); c >= 0; c--)
            {
                value += (column!.ToUpper()[c] - 64) * (int)Math.Pow(26, c+1);
            }

            return value;
        }

        public void Write(string documentId, string range, IList<IList<object>> values)
        {
            var lines = new List<string>();
            foreach(IList<object> row in values)
            {
                lines.Add(string.Join(",", row));
            }

            File.WriteAllLines(documentId, lines.ToArray());
        }
    }
}
