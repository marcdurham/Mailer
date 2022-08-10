using Microsoft.AspNetCore.Mvc;
using ScheduleViewer.EmailDataServices;

namespace ScheduleViewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailDataController : ControllerBase
    {
        private readonly IEmailDataService _emailDataService;
        private readonly ILogger<EmailDataController> _logger;

        public EmailDataController(
            IEmailDataService emailDataService,
            ILogger<EmailDataController> logger)
        {
            _emailDataService = emailDataService;
            _logger = logger;
        }

        [HttpGet]
        public EmailData Get(string date)
        {
            return _emailDataService.Get(date);
        }
    }
}
