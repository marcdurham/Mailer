using Microsoft.AspNetCore.Mvc;
using ScheduleViewer.Models;

namespace ScheduleViewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmailDataController : ControllerBase
    {
        private readonly ILogger<EmailDataController> _logger;

        public EmailDataController(ILogger<EmailDataController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<EmailData> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new EmailData
            { 
                Name = $"Number {index}"
            })
            .ToArray();
        }
    }
}
