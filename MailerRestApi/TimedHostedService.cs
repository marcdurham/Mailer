using GoogleAdapter.Adapters;
using MailerCommon.Configuration;
using MailerRestApi;
using MailerRestApi.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Mailer.Sender;
public class TimedHostedService : IHostedService, IDisposable
{
    private int executionCount = 0;
    private readonly ILogger<PublisherEmailer> _logger;
    private Timer _timer = null!;
    private readonly int _intervalSeconds = 60;
    private readonly IScheduleService _scheduleService;
    private readonly IConfiguration Configuration;
    private readonly IMemoryCache _memoryCache;
    public TimedHostedService(
        IScheduleService scheduleService,
        IConfiguration configuration, 
        ILogger<PublisherEmailer> logger, 
        IMemoryCache memoryCache)
    {
        _scheduleService = scheduleService;
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
        _logger.LogInformation("Running schedule service");

        _scheduleService.Run();
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