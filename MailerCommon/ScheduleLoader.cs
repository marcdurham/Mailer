namespace MailerCommon
{
    public class ScheduleLoader
    {
        public static List<Meeting> GetSchedule(
            IList<IList<object>> values, 
            Dictionary<string, Friend> friendMap, 
            int[] daysOfWeek, 
            string title, 
            int mondayColumnIndex = 0)
        {
            const int HeaderRowIndex = 0;

            string[] headers = new string[values[HeaderRowIndex].Count];
            var assignmentNames = new Dictionary<string, string>();
            for (int col = 0; col < values[HeaderRowIndex].Count; col++)
            {
                string assignmentName = values[HeaderRowIndex][col]?.ToString() ?? string.Empty;
                headers[col] = assignmentName;
                assignmentNames[assignmentName.ToUpper()] = assignmentName;
            }

            var meetings = new List<Meeting>();

            string[] rows = new string[values.Count];
            for (int wk = 1; wk < values.Count; wk++)
            {
                rows[wk] = values[wk][mondayColumnIndex]?.ToString() ?? string.Empty;
                var monday = DateTime.Parse(values[wk][mondayColumnIndex].ToString() ?? string.Empty);
                var meeting = new Meeting
                {
                    Name = title,
                    Date = monday.AddDays(daysOfWeek[0])
                };

                for (int a = 2; a < values[wk].Count && a < headers.Length; a++)
                {
                    string assigneeName = values[wk][a]?.ToString() ?? string.Empty;
                    Friend assignee;
                    if (friendMap.ContainsKey(assigneeName.ToUpperInvariant()))
                    {
                        assignee = friendMap[assigneeName.ToUpperInvariant()];
                    }
                    else
                    {
                        assignee = new MissingFriend(assigneeName);
                    }

                    string assignementKey = headers[a];
                    var assignment = new Assignment
                    {
                        Key = assignementKey,
                        Name = assignmentNames[assignementKey.ToUpper()],
                        Date = meeting.Date,
                        School = 0,
                        Friend = assignee,
                        Meeting = meeting.Name,
                        MeetingName = meeting.Name == "CLM" 
                            ? "CLM"
                            : (meeting.Name == "PW" ? "PT/WS" :(meeting.Name == "MFS" ? "Service" : ""))
                    };

                    meeting.Assignments[assignment.Key] = assignment;
                }

                meetings.Add(meeting);
            }

            return meetings;
        }
    }
}
