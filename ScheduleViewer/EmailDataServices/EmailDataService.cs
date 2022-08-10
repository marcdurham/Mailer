using Microsoft.Extensions.Caching.Memory;

namespace ScheduleViewer.EmailDataServices;

public interface IEmailDataService
{
    EmailData Get(string date, string key);
}

public class EmailDataService : IEmailDataService
{
    private readonly ISpreadSheetService _sheetService;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailDataService> _logger;

    public EmailDataService(
        ISpreadSheetService sheetService, 
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<EmailDataService> logger)
    {
        _sheetService = sheetService;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _logger = logger;
    }

    public EmailData Get(string date, string key)
    {
        EmailData emailData = new()
        {
            Key = key,
        };

        List<string> authorizedKeys = _configuration
            .GetSection("AuthorizedKeys")
            .Get<List<string>>()
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.ToUpperInvariant())
            .ToList();

        if (string.IsNullOrWhiteSpace(key) 
            || key == "null" 
            || authorizedKeys.Count == 0 
            || !authorizedKeys.Contains(key.ToUpperInvariant()))
        {
            _logger.LogError($"Invalid key {key}");
            throw new UnauthorizedAccessException();
        }

        if (string.IsNullOrWhiteSpace(date) || date == "null")
        {
            DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
            date = thisMonday.ToString("yyyy-MM-dd");
        }

        emailData.Date = date;

        string documentId = _configuration.GetValue<string>("EmailDataDocumentId");
        string range = _configuration.GetValue<string>("EmailDataRange");

        IList<IList<object>>? values = null!;
        if (!_memoryCache.TryGetValue($"{documentId}/{range}", out IList<IList<object>> v))
        {
            _logger.LogInformation("Reading from Google Sheets...");
            values = _sheetService.Read(documentId, range);
            _memoryCache.Set($"{documentId}/{range}", values);
        }
        else
        {
            _logger.LogInformation("Reading from Memory Cache...");
            values = v;
        }

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

            if (DateTime.TryParse(columnDate, out DateTime coldt))
            {
                if (coldt.DayOfWeek == DayOfWeek.Monday)
                {
                    emailData.Previous = coldt.AddDays(-2).ToString("yyyy-MM-dd");
                    emailData.Next = coldt.AddDays(5).ToString("yyyy-MM-dd");
                }
                else
                {
                    emailData.Previous = coldt.AddDays(-5).ToString("yyyy-MM-dd");
                    emailData.Next = coldt.AddDays(2).ToString("yyyy-MM-dd");
                }
            }

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
        
        emailData.Success = true;

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
