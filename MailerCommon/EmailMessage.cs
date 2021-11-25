namespace MailerCommon;
public class EmailMessage
{
    public string FromAddress { get; set; }
    public string FromName { get; set; }
    public string ToName { get; internal set; }
    public string ToAddress { get; internal set; }
    public string Subject { get; internal set; }
    public string Text { get; internal set; }
}
