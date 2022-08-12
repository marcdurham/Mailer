using Microsoft.AspNetCore.Mvc;
using ScheduleViewer.EmailDataServices;
using ScheduleViewer.Models;

namespace ScheduleViewer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SiteInfoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SiteInfoController> _logger;

        public SiteInfoController(
            IConfiguration configuration,
            ILogger<SiteInfoController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<EmailData> Get()
        {
            try
            {
                var info = new SiteInfo()
                {
                    SiteName = _configuration.GetValue<string>("SiteName")
                };

                return Ok(info);
            }
            catch(Exception)
            {
                throw;
            }
        }
    }
}
