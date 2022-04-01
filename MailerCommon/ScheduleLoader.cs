namespace MailerCommon
{
    public class ScheduleLoader
    {
        public static List<Meeting> GetSchedule(
            IList<IList<object>> values, 
            Dictionary<string, Friend> friendMap, 
            int[] daysOfWeek, 
            string name,
            string title, 
            TimeOnly? meetingStartTime,
            int mondayColumnIndex = 0,
            int meetingDateColumnIndex = 0)
        {
            const int HeaderRowIndex = 0;

            if(values == null || values.Count == 0)
                return new List<Meeting>();

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
                //var monday = DateTime.Parse(values[wk][mondayColumnIndex].ToString() ?? string.Empty);
                var meetingDay = DateTime.Parse(values[wk][meetingDateColumnIndex].ToString() ?? string.Empty);
                var meeting = new Meeting
                {
                    Name = name,
                    Title = title,
                    Date = meetingDay.AddTicks(meetingStartTime.HasValue ? meetingStartTime.Value.Ticks : 0),
                };

                for (int a = 0; a < values[wk].Count && a < headers.Length; a++)
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
                    

                    Assignment assignment = MapAssignment(headers, values, assignmentNames, wk, meeting, assignee, assignementKey);

                    meeting.Assignments[assignment.Key] = assignment;
                }

                meetings.Add(meeting);
            }

            return meetings;
        }

        private static Assignment MapAssignment(
            string[] headers,
            IList<IList<object>> values, 
            Dictionary<string, string> assignmentNames, 
            int wk, 
            Meeting meeting, 
            Friend assignee, 
            string assignmentKey)
        {
            int indexOfStart = HeaderArray.StartColumnIndexOf(headers, assignmentKey);

            TimeOnly start = indexOfStart >= 0
                    && wk < values.Count
                    && indexOfStart < values[wk].Count
                    && values[wk][indexOfStart] != null
                    && !string.IsNullOrWhiteSpace(values[wk][indexOfStart].ToString())
                ? TimeOnly.Parse(values[wk][indexOfStart].ToString()!)
                : TimeOnly.MinValue;

            return new Assignment
            {
                Key = assignmentKey,
                Name = assignmentNames[assignmentKey.ToUpper()],
                Date = meeting.Date,
                Start = start,
                School = 0,
                Friend = assignee,
                Meeting = meeting.Name,
                MeetingTitle = meeting.Title
            };
        }
    }
}
