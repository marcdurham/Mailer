using GoogleAdapter.Adapters;
using Mailer.Sender;
using MailerCommon.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace MailerRestApi.Services
{
    public interface IScheduleService
    {
        void Run();
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
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation($"Generating schdules");

            string? friendInfoDocumentId = _configuration.GetValue<string>("FriendInfoDocumentId");
            string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
            string googleApiSecretsJson = File.ReadAllText("./GoogleApi.secrets.json");
            ISheets sheets = new GoogleSheets(googleApiSecretsJson);

            //var schedules = Configuration.GetSection("Schedules").GetValue<ScheduleInputs[]>("Schedules");
            var scheduleOptions = new ScheduleOptions();

            _configuration.GetSection("Schedules").Bind(scheduleOptions);
            var schedules = scheduleOptions.Schedules;

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
