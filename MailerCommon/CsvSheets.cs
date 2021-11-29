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

            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture);
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

            string startingColumn = match.Groups[4].Value;
            using (var reader = new StreamReader(documentId))
            using (var csv = new CsvReader(reader, configuration))
            {
                var records = csv.GetRecords<dynamic>().ToList<dynamic>();
                
                var output = new List<IList<object>>();
                foreach(IDictionary<string, object> record in records)
                {
                    var row = new List<object>();
                    var keys = record.Keys.ToArray();
                    for(int i = 0; i < record.Keys.Count; i++) //string key in record.Keys)
                    {
                        //string key = record.Keys[i];
                        string key = keys[i];
                        //KeyValuePair<string, object> item = record[key];
                        //object item = record[key];
                        if (key.StartsWith("MISSING-"))
                            continue;

                        //row.Add(item.Value);
                        row.Add(record[key]);
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
