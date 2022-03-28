using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspNet6.Pages;

public class MeetingsModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;

    public MeetingsModel(ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public string RawHtml { get; set; } = String.Empty;

    public IActionResult OnGet(string name)
    {
        string scheduleFolder = _configuration.GetValue<string>("Schedules:StaticScheduleRootFolder")!;
        string schedFilePath = $"{Path.Combine(scheduleFolder, name)}.html";
        if (!System.IO.File.Exists(schedFilePath))
        {
            return NotFound();
        }

        RawHtml = System.IO.File.ReadAllText(schedFilePath);

        return Page();
    }
}
