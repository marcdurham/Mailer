public class ScheduleConfiguration
{
    public List<string> RowNameKeys { get; set; } = new();
    public List<string> RowNameTitles { get; set; } = new();
    public List<string> AuthorizedKeys { get; set; } = new();
    public string EmailDataRange { get; set; } = string.Empty;
    public string EmailDataDocumentId { get; set; } = string.Empty;
}
