namespace MailerCommon
{
    public interface ICustomLogger<T>
    {
        void LogInformation(string message);
    }
}
