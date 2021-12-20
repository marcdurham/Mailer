using MailerCommon;

namespace MailerRestApi
{
    public class CustomLogger : ICustomLogger
    {
        private readonly ILogger _logger;

        public CustomLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }
    }
}
