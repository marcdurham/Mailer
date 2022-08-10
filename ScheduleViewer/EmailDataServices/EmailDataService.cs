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
                if(/*!string.IsNullOrWhiteSpace(emailData.Saturdays[i])
                    &&*/ emailData.Mondays[i] == date)
                {
                    dateColumIndex = i;
                    break;
                }
            }

            for (int n = 0; n < emailData.RowNames.Count && n < values.Count; n++)
            {
                emailData.Rows.Add(
                    new AssignmentRow
                    {
                        Name = emailData.RowNames[n],
                        Value = $"{(values[n].Count > dateColumIndex ? values[n][dateColumIndex] : null)}",
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
