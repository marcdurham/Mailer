namespace Mailer.Sender;

public class EmailRecipient
{
    public string? Name { get; set; }
    public string? EmailAddress { get; set; }
    public string? EmailAddressFromFriend { get; set;  }
    public string? Sent { get; set; }
    public string? Result { get; set; }

    public override string ToString()
    {
        return $"{Name} {EmailAddress} {Sent} {Result}";
    }
}
