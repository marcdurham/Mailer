﻿namespace MailerCommon
{
    public class ScheduleLoader
    {
        public static List<Meeting> GetSchedule(IList<IList<object>> values, Dictionary<string, Friend> friendMap, int dayOfWeek, string title, int weekKeyColumnIndex = 0)
        {
            const int HeaderRowIndex = 0;
            //DateTime thisMonday = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));

            string[] headers = new string[values[HeaderRowIndex].Count];
            var assignmentNames = new Dictionary<string, string>();
            for (int col = 0; col < values[HeaderRowIndex].Count; col++)
            {
                string assignmentName = values[HeaderRowIndex][col]?.ToString() ?? string.Empty;
                headers[col] = assignmentName;
                assignmentNames[assignmentName.ToUpper()] = assignmentName;
            }

            //var schedule = new Schedule()
            //{
            //    NextMeetingDate = thisMonday.AddDays(3),   // different
            //};
            var meetings = new List<Meeting>();

            string[] rows = new string[values.Count];
            for (int wk = 1; wk < values.Count; wk++)
            {
                rows[wk] = values[wk][weekKeyColumnIndex]?.ToString() ?? string.Empty;
                var monday = DateTime.Parse(values[wk][weekKeyColumnIndex].ToString() ?? string.Empty);
                var meeting = new Meeting
                {
                    Name = title,              // different
                    Date = monday.AddDays(dayOfWeek)   // different
                };

                //var week = new ScheduleWeek
                //{
                //    Start = monday,
                //    Midweek = meeting // different
                //};

                for (int a = 2; a < values[wk].Count; a++)
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
                    };

                    meeting.Assignments[assignment.Key] = assignment;
                }

                meetings.Add(meeting);
                //schedule.Weeks.Add(week);
            }

            //return schedule;
            return meetings;
        }
    }
}
