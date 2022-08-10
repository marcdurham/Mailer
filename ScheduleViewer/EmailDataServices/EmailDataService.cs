namespace ScheduleViewer.EmailDataServices
{
    public interface IEmailDataService
    {
        EmailData Get(string date);
    }

    public class EmailDataService : IEmailDataService
    {
        private readonly ISpreadSheetService _sheetService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailDataService> _logger;

        public EmailDataService(
            ISpreadSheetService sheetService, 
            IConfiguration configuration,
            ILogger<EmailDataService> logger)
        {
            _sheetService = sheetService;
            _configuration = configuration;
            _logger = logger;
        }

        public EmailData Get(string date)
        {
            string documentId = _configuration.GetValue<string>("EmailDataDocumentId");
            string range = _configuration.GetValue<string>("EmailDataRange");
            IList<IList<object>>? values = _sheetService.Read(documentId, range);

            EmailData emailData = new();
            for(int r = 0; r < values.Count; r++)
            {
                IList<object>? row = values[r];
                string name = $"{(row.Count > 0 ? row[0] : "")}";
                if (string.IsNullOrEmpty(name))
                    break;

                if(emailData.RowNames.Contains(name))
                {
                    _logger.LogError($"Duplicate Row Name {name} at row {r}");
                    break;
                }

                emailData.RowNames.Add(name);
                //emailData.Rows.Add(new EmailDataRow { Name = name });
            }

            int weekMondayRowIndex = FindRequiredRowIndex(emailData.RowNames, "week_monday");
            int weekSaturdayRowIndex = FindRequiredRowIndex(emailData.RowNames, "week_saturday");

            //List<int> mondays = new();
            for (int i = 0; i < values[weekMondayRowIndex].Count; i++)
            {
                emailData.Mondays.Add($"{values[weekMondayRowIndex][i]}");
            }
            for (int i = 0; i < values[weekSaturdayRowIndex].Count; i++)
            {
                emailData.Saturdays.Add($"{values[weekSaturdayRowIndex][i]}");
            }

            int dateColumIndex = 0;
            for(int i = 0; i < emailData.Saturdays.Count && i < emailData.Mondays.Count; i++)
            {
                string columnDate = string.IsNullOrWhiteSpace(emailData.Mondays[i]) 
                    ? emailData.Saturdays[i]
                    : emailData.Mondays[i];

                if (columnDate == date)
                {
                    dateColumIndex = i;
                    break;
                }
            }

            var rowNamesToShow = _configuration.GetSection("RowNameKeys").Get<List<string>>();
            var rowTitles = _configuration.GetSection("RowNameTitles").Get<List<string>>();

            for (int n = 0; n < emailData.RowNames.Count && n < values.Count; n++)
            {
                string rowName = emailData.RowNames[n];
                if (!rowNamesToShow.Contains(rowName))
                    continue;

                string title = rowTitles[rowNamesToShow.IndexOf(rowName)] ?? "";
                
                if(title.StartsWith("$") && emailData.RowNames.Contains(title.Substring(1)))
                {
                    string titleKey = title.Substring(1);
                    int titleIndex = emailData.RowNames.IndexOf(titleKey);
                    //title = rowTitles[titleIndex] ?? rowName;
                    title = $"{(values[titleIndex].Count > dateColumIndex ? values[titleIndex][dateColumIndex] : null)}";
                }

                string value = $"{(values[n].Count > dateColumIndex ? values[n][dateColumIndex] : null)}";
                if (string.IsNullOrEmpty(value))
                    continue;

                emailData.Rows.Add(
                    new AssignmentRow
                    {
                        Name = title,
                        Value = value,
                    });
            }

            return emailData;
        }

        int FindRequiredRowIndex(List<string> rowNames, string name)
        {
            int index = rowNames.IndexOf(name);
            if (index < 0)
            {
                string message = $"Cannot find {name} row!";
                _logger.LogError(message);
                throw new Exception(message);
            }
            return index;
        }
    }
}
