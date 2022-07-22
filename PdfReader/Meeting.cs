namespace PdfReader
{
    public class Meeting
    {
        public string WeekHeader { get; set; }
        public List<Assignment> Assignments { get; set; } = new();
    }
}
