using MailerCommon.Configuration;

namespace MailerCommon
{
    public class ScheduleLoader
    {
        public static List<Meeting> GetSchedule(
            IList<IList<object>> sheet, 
            Dictionary<string, Friend> friendMap, 
            ScheduleInputs scheduleInputs)
        {
            const int HeaderRowIndex = 0;
            string name = scheduleInputs.MeetingName;
            string title = scheduleInputs.MeetingTitle;
            TimeOnly? meetingStartTime = scheduleInputs.MeetingStartTime.HasValue
                ? TimeOnly.FromDateTime((DateTime)scheduleInputs.MeetingStartTime)
                : null;
            int mondayColumnIndex = 0;
            int meetingDateColumnIndex = scheduleInputs.MeetingDateColumnIndex ?? 0;

            // Rotate sheet if needed
            IList<IList<object>> values = scheduleInputs.ColumnIsDay 
                ? Rotate(sheet, HeaderRowIndex)
                : sheet;

            if (values == null || values.Count == 0)
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
                DateTime meetingDay = DateTime.Parse(values[wk][meetingDateColumnIndex].ToString() ?? string.Empty);

                var meeting = new Meeting
                {
                    Name = name,
                    Title = title,
                    Date = meetingDay.AddTicks(meetingStartTime.HasValue ? meetingStartTime.Value.Ticks : 0),
                    HasMultipleMeetingsPerWeek = scheduleInputs.HasMultipleMeetingsPerWeek,
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

        private static IList<IList<object>> Rotate(IList<IList<object>> sheet, int headerRowIndex)
        {
            IList<IList<object>> values = sheet;
            values = new List<IList<object>>();
            int rowCount = sheet.Count;
            int colCount = sheet[headerRowIndex].Count;
            // Add columns as rows
            for (int c = headerRowIndex; c < colCount; c++)
            {
                values.Add(new List<object>());
            }

            // Add each column c to each row r               
            for (int c = headerRowIndex; c < colCount; c++)
            {
                for (int r = 0; r < rowCount; r++)
                {
                    if (sheet[r].Count > c)
                    {
                        values[c].Add(sheet[r][c]);
                    }
                    else
                    {
                        values[c].Add(null!);
                    }
                }
            }

            return values;
        }

        static Assignment MapAssignment(
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
