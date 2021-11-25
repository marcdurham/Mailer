namespace Mailer.Sender;
public class Message
{
    public string ToAddress { get; set;}
    public string ToName { get; set; }
    public string Subject { get; set; }
    public string Text { get; set; }
    public string FromName { get; internal set; }
    public string FromAddress { get; internal set; }
}