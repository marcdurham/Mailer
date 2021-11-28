using CsvHelper;
using GoogleAdapter.Adapters;
using System.Globalization;

namespace MailerCommon
{
    public class CsvSheets : ISheets
    {
        public IList<IList<object>> Read(string documentId, string range)
        {
            using (var reader = new StreamReader(documentId))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                dynamic records = csv.GetRecords<dynamic>();
                
                var output = new List<IList<object>>();
                foreach(dynamic record in records)
                {
                    var row = new List<object>();
                    foreach(object item in record)
                    {
                        row.Add(item);
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
