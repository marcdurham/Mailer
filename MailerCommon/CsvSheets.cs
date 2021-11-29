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

            if (startColumnString.Length != 1)
                throw new ArgumentException($"Starting column {startColumnString} can only be one character wide, A thru Z. AA and higher is not permitted.");

            int startColumn = startColumnString[0] - 'A';

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

                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                    var row = new List<object>();
                    for(int col = startColumn; col < csv.HeaderRecord.Count(); col++)
                    {
                        object val = csv[col];
                        row.Add(csv[col]);
                    }

                    output.Add(row);
                }

                return output;
            }
        }

        public void Write(string documentId, string range, IList<IList<object>> values)
        {
            throw new NotImplementedException();
        }
    }
}
