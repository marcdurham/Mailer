namespace MailerCommon;

public class Friend
{
    public readonly static Friend Nobody = new Nobody();
    public bool IsMissing { get; set; } = true;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EnglishName => Name;
    public string PinYinName { get; set; } = string.Empty;
    public string SimplifiedChineseName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string[] AllNames() => new string[] { PinYinName, SimplifiedChineseName, EnglishName };
    public override string ToString()
    {
        return $"{Name} {EmailAddress}";
    }
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
        Name = name;
        IsMissing = true;
    }

    public override string ToString()
    {
        return $"Missing: {base.ToString()}";
    }
}