namespace MailerCommon;

public class Friend
{
    public readonly static Friend Nobody = new Nobody();
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PinYinName { get; set; } = string.Empty;
    public string SimplifiedChineseName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
}

public class Nobody : Friend
{
    public Nobody()
    {
        Name = "Nobody";
    }
}

public class MissingFriend : Friend
{
    public MissingFriend(string name)
    {
        Name = $"*{name}*";
    }
}