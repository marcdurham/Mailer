using MailerCommon;

namespace Mailer.Sender;

public class EmailRecipient
{
    public string? Name { get; set; }
    public string? EmailAddress { get; set; }
    public string? EmailAddressFromFriend { get; set; }
    public Friend Friend { get; set; }
    public string? Sent { get; set; }
    public string SentStatus { get; internal set; }
    public string Check { get; internal set; }
    public string CheckStatus { get; internal set; }

    public override string ToString()
    {
        return $"{Name} {EmailAddress} {Sent} {CheckStatus}";
    }
}
