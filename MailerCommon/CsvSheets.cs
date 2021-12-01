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
            (int startRow, int endRow, int startColumn, int endColumn) = ParseCellRange(range);

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                ShouldSkipRecord = row => false,

            };
            configuration.PrepareHeaderForMatch = (h) =>
            {
                if (string.IsNullOrWhiteSpace(h.Header))
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
                for (int col = startColumn; col <= endColumn && col < headerCount; col++)
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
                    for (int col = startColumn; col <= endColumn && col < headerCount; col++)
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

        static (int startRow, int endRow, int startColumn, int lastColumn) ParseCellRange(string range)
        {
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

            return (startRow, endRow, startColumn, lastColumn);
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
            (int startColumn, _, int startRow,_) = ParseCellRange(range);

            var lines = new List<string>();
            if(File.Exists(documentId))
            {
                string[] content = File.ReadAllLines(documentId);
                for(int i =0; i < startRow; i++)
                    lines.Add(content[i]);
            }

            foreach(IList<object> row in values)
            {
                string?[] line = new string[startColumn + row.Count];
                for(int i = 0; i < startColumn; i++)
                    line[i] = string.Empty;

                for(int i = startColumn; i < (startColumn + row.Count); i++)
                    line[i] = row[i-startColumn]?.ToString();

                lines.Add(string.Join(",", line));
            }

            File.WriteAllLines(documentId, lines.ToArray());
        }
    }
}
