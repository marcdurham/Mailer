using MailerCommon;

namespace MailerRestApi
{
    public class CustomLogger<T> : ICustomLogger<T>
    {
        private readonly ILogger<T> _logger;

        public CustomLogger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }
    }
}
