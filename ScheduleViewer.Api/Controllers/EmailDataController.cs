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
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public ActionResult<EmailData> Get(string date, string key)
        {
            try
            {
                return Ok(_emailDataService.Get(date, key));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid key");
            }
            catch(Exception)
            {
                throw; 
            }
        }
    }
}
