using GoogleAdapter.Adapters;
using MailerCommon.Configuration;
using MailerRestApi;
using Microsoft.Extensions.Caching.Memory;

namespace Mailer.Sender;
public class TimedHostedService : IHostedService, IDisposable
{
    private int executionCount = 0;
    private readonly ILogger<PublisherEmailer> _logger;
    private Timer _timer = null!;
    private readonly int _intervalSeconds = 60;
    private readonly IConfiguration Configuration;
    private readonly IMemoryCache _memoryCache;
    public TimedHostedService(IConfiguration configuration, ILogger<PublisherEmailer> logger, IMemoryCache memoryCache)
    {
        Configuration = configuration;
        _intervalSeconds = int.Parse(Configuration["TimerIntervalSeconds"]);
        _logger = logger;
        _memoryCache = memoryCache;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service running.");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, 
            TimeSpan.FromSeconds(_intervalSeconds));

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        var count = Interlocked.Increment(ref executionCount);

        _logger.LogInformation($"Timed Hosted Service is working. Interval (sec): {_intervalSeconds} Count: {count}");

        string? friendInfoDocumentId = Configuration.GetValue<string>("FriendInfoDocumentId");
        //string? friendInfoDocumentId = Environment.GetEnvironmentVariable("FriendInfoDocumentId", EnvironmentVariableTarget.Process);
        string? sendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY", EnvironmentVariableTarget.Process);
        string googleApiSecretsJson = File.ReadAllText("./GoogleApi.secrets.json");
        ISheets sheets = new GoogleSheets(googleApiSecretsJson);

        //var schedules = Configuration.GetSection("Schedules").GetValue<ScheduleInputs[]>("Schedules");
        var scheduleOptions = new ScheduleOptions();
        Configuration.GetSection("Schedules").Bind(scheduleOptions);
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
                friendInfoDocumentId: friendInfoDocumentId,
                schedules: schedules.ToList());
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Hosted Service is stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}