namespace ScheduleViewer.EmailDataServices
{
    public class EmailData 
    {
        public string Date { get; set; } = string.Empty;
        public string Previous { get; set; } = string.Empty;
        public string Next { get; set; } = string.Empty;
        public List<string> RowNames { get; set; } = new();
        public List<AssignmentRow> Rows { get; set; } = new();
        public List<string> Mondays { get; set; } = new();
        public List<string> Saturdays { get; set; } = new();
    }
}

