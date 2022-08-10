namespace ScheduleViewer.EmailDataServices
{
    public class EmailData 
    {
        public List<string> RowNames { get; set; } = new();
        public List<AssignmentRow> Rows { get; set; } = new();
        public List<string> Mondays { get; set; } = new();
        public List<string> Saturdays { get; set; } = new();
    }
}

