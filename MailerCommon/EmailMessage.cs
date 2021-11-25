namespace MailerCommon;
public class EmailMessage
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
