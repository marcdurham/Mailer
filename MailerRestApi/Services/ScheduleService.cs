using GoogleAdapter.Adapters;
using Mailer.Sender;
using MailerCommon.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace MailerRestApi.Services
{
    public interface IScheduleService
    {
        void Run();
        void Run(string meetingName);
    }

    public class ScheduleService : IScheduleService
    {
        private int executionCount = 0;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PublisherEmailer> _logger;
        private readonly IMemoryCache _memoryCache;

        public ScheduleService(IConfiguration configuration, ILogger<PublisherEmailer> logger, IMemoryCache memoryCache)
        {
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public void Run()
        {
            RunSchedule("all");
        }
        public void Run(string meetingName)
        {
            RunSchedule(meetingName);
        }

        void RunSchedule(string meetingName)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation($"Generating schdules");

            string? friendInfoDocumentId = _configuration.GetValue<string>("FriendInfoDocumentId");
            string? sendGridApiKey = _configuration.GetValue<string>("SENDGRID_API_KEY");
            string googleApiSecretsJson = File.ReadAllText("./GoogleApi.secrets.json");
            ISheets sheets = new GoogleSheets(googleApiSecretsJson);

            //var schedules = Configuration.GetSection("Schedules").GetValue<ScheduleInputs[]>("Schedules");
            var scheduleOptions = new ScheduleOptions();

            _configuration.GetSection("Schedules").Bind(scheduleOptions);
            ScheduleInputs[] schedules = scheduleOptions.Schedules
                .Where(s => 
                    string.Equals("all", meetingName, StringComparison.OrdinalIgnoreCase) 
                    || string.Equals(s.MeetingName, meetingName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            _logger.LogInformation($"Schedules Count: {schedules.Length}");

            new PublisherEmailer(
                scheduleOptions,
                new CustomLogger<PublisherEmailer>(_logger),
                _memoryCache,
                sheets,
                sendGridApiKey,
                dryRunMode: false,
                forceSendAll: false)
                .Run(
                    utcNow: DateTime.UtcNow,
                    friendInfoDocumentId: friendInfoDocumentId,
                    schedules: schedules.ToList());
        }
    }
}

