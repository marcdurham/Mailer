namespace PdfReader;

public class Assignment
{
    public string WeekHeader { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public TimeOnly Start { get; set; } = new TimeOnly();
    public string Section { get; set; } = string.Empty;
    public string AssignmentName { get; set; }
    public string AssigneeName { get; set; }
    public string AssigneeSecondaryName { get; set; }
    public string AssigneeNameChinese { get; set; }
    public string AssigneeNameEnglish { get; set; }
}
