using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;

namespace PdfReader
{
    public class Pdf
    {
        public string Read(string filePath)
        {
            var builder = new StringBuilder(10000);
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                List<Assignment> assignments = new();
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    // This starts at 1 rather than 0.
                    var page = document.GetPage(i + 1);
                    var words = page.GetWords().ToList();
                    List<List<Word>> lines = new();
                    List<Word> line = new();
                    List<Line> lineObjects = new();
                    Line lineObject = new();
                    for (int w = 0; w < words.Count; w++)
                    {
                        Word word = words[w];
                        line.Add(word);
                        lineObject.Boxes.Add(
                            new Box
                            {
                                Text = word.Text,
                            });

                        if (word.BoundingBox.Left > NextWordBoundingBox(w, words).Left)
                        {
                            lines.Add(line);
                            lineObjects.Add(lineObject);
                            line = new();
                            lineObject = new();
                        }
                    }

                    for (int w = 0; w < words.Count; w++)
                    {
                        Word word = words[w];
                        //Console.WriteLine(word.Text);
                        builder.Append($"Left: {word.BoundingBox.Left:0000.00}, Top: {word.BoundingBox.Top:0000.00},  Width: {word.BoundingBox.Width:000.00}, Height: {word.BoundingBox.Height:000.00}:  ");
                        builder.AppendLine(word.Text);
                        if (string.Equals(word.Text, "禱告："))
                        {
                            builder.AppendLine($"PRAYER: {NextWordText(w, words)}");
                        }
                        else if (string.Equals(word.Text, "主席："))
                        {
                            builder.AppendLine($"CHAIRMAN: {NextWordText(w, words)}");
                        }
                        else if (string.Equals(word.Text, "第二班導師："))
                        {
                            builder.AppendLine($"2ND_SCHOOL_COUNSELOR: {NextWordText(w, words)}");
                        }

                        if(word.BoundingBox.Left > NextWordBoundingBox(w, words).Left)
                        {
                            builder.AppendLine("NEW_LINE");
                        }

                        if(Regex.IsMatch(word.Text, @"\d+:\d\d"))
                        {
                            builder.AppendLine($"START: {word.Text}");
                        }
                    }

                    builder.AppendLine("All Text");
                    builder.Append(page.Text);

                    builder.AppendLine("LINES:");
                    //for(int l = 0; l < lines.Count; l++)
                    string weekHeader = string.Empty;
                    string sectionHeader = string.Empty;
                    foreach(Line lin in lineObjects)
                    {
                        builder.AppendLine($"{lineObjects.IndexOf(lin)}:: {lin.AllText()}");
                        if(IsWeekHeader(lin) && lin.ContainsBoxText("|"))
                        {
                            weekHeader = lin.AllText().Split('|')[0];
                            sectionHeader = string.Empty;
                        }

                        if(IsSectionHeader(lin))
                        {
                            sectionHeader = lin.Boxes[0].Text;
                        }

                        foreach(var box in lin.Boxes)
                        {
                            builder.AppendLine(box.Text);
                        }

                        assignments.AddRange(Process(lin, sectionHeader, weekHeader));

                        //foreach(Assignment assignment in assignments)
                        //{
                        //    builder.AppendLine($"ASSIGNMENT: WK: {assignment.WeekHeader} SEC: {assignment.Section} ST: {assignment.Start} MIN: {assignment.Minutes} AN: {assignment.AssignmentName} To: {assignment.AssigneeName}");
                        //}
                        builder.AppendLine();
                    }

                }
                builder.AppendLine("ALL ASSIGNMENTS:");

                foreach (Assignment assignment in assignments)
                {
                    builder.AppendLine($"WK: {assignment.WeekHeader} SEC: {assignment.Section} ST: {assignment.Start} MIN: {assignment.Minutes} AN: {assignment.AssignmentName} To: {assignment.AssigneeName}");
                }
            }

            return builder.ToString();
        }

        bool IsWeekHeader(Line line)
        {
            return line.Boxes.Count > 4
                && Regex.IsMatch(line.Boxes[0].Text, @"^\d+$") 
                && line.Boxes[1].Text == "月"
                && Regex.IsMatch(line.Boxes[2].Text, @"\d+")
                && line.ContainsBoxText("日");
        }

        bool IsSectionHeader(Line line)
        {
            return line.Boxes.Count > 0 && (
                   line.Boxes[0].Text == "上帝話語的寶藏"
                || line.Boxes[0].Text == "用心準備傳道工作"
                || line.Boxes[0].Text == "基督徒的生活");
        }

        List<Assignment> Process(Line line, string sectionHeader, string weekHeader)
        {
            List<Assignment> assignments = new();
            var startlessPattern = new Regex(@"(主席|第二班導師)：(.*)");
            var startlessMatch = startlessPattern.Match(line.AllText());
            if (startlessMatch.Success)
            {
                Assignment assignment = new()
                {
                    WeekHeader = weekHeader,
                    Section = sectionHeader,
                    AssignmentName = startlessMatch.Groups[1].Value,
                    AssigneeName = startlessMatch.Groups[2].Value,
                };

                assignments.Add(assignment);
            }

            var prayerPattern = new Regex(@"(\d+:\d\d)•(.*)(禱告)：(.*)");
            var prayerMatch = prayerPattern.Match(line.AllText());
            if (prayerMatch.Success)
            {
                Assignment assignment = new()
                {
                    WeekHeader = weekHeader,
                    Section = sectionHeader,
                    Start = TimeOnly.Parse(prayerMatch.Groups[1].Value),
                    AssignmentName = prayerMatch.Groups[3].Value,
                    AssigneeName = prayerMatch.Groups[4].Value,
                };

                assignments.Add(assignment);
            }

            if (line.Boxes.Count > 3 && Regex.IsMatch(line.Boxes[0].Text, @"^\d+:\d\d$"))
            {
                string assignmentName = line.Boxes[2].Text;
                int minutes = 0;
                string assigneeName = string.Empty;
                var minutesPattern = new Regex(@"\d+:\d\d•(.*)[（(](\d+)分鐘[）)](.*)");
                var match = minutesPattern.Match(line.AllText());
                if(match.Success)
                {
                    assignmentName = match.Groups[1].Value;
                    string minutePart = match.Groups[2].Value;
                    minutes = int.Parse(minutePart);
                    assigneeName = match.Groups[3].Value;
                }

                Assignment assignment = new()
                {
                    WeekHeader = weekHeader,
                    Section = sectionHeader,
                    Start = TimeOnly.Parse(line.Boxes[0].Text),
                    AssignmentName = assignmentName,
                    AssigneeName = assigneeName,
                    Minutes = minutes
                };

                if(assignment.AssignmentName == "唱詩第")
                {
                    assignment.AssignmentName = $"唱詩第{line.Boxes[3].Text}";
                }
                else if(assignment.AssignmentName == "會眾研經班")
                {
                    assignment.AssigneeName = assignment.AssigneeName.Replace("主持人/朗讀員：", "");
                    if(assignment.AssigneeName.Contains("/"))
                    {
                        Assignment secondAssignment = new()
                        {
                            WeekHeader = assignment.WeekHeader,
                            Section = sectionHeader,
                            Start = assignment.Start,
                            AssignmentName = $"{assignment.AssignmentName} 朗讀員",
                            AssigneeName = assignment.AssigneeName.Split("/", StringSplitOptions.TrimEntries)[1],
                            Minutes = assignment.Minutes
                        };

                        assignment.AssigneeName = assignment.AssigneeName.Split("/", StringSplitOptions.TrimEntries)[0];
                        assignment.AssignmentName = $"{assignment.AssignmentName} 主持人";
                        assignments.Add(secondAssignment);
                    }
                }
                else if(assignment.AssigneeName.StartsWith("學生/助手："))
                {
                    assignment.AssigneeName = assignment.AssigneeName.Replace("學生/助手：", "");
                }

                assignments.Add(assignment);
            }

            return assignments;
        }

        public string NextWordText(int w, List<Word> words)
        {
            if (w < (words.Count - 1) && words[w].BoundingBox.Left < words[w + 1].BoundingBox.Left)
            {
                return words[w + 1].Text;
            }
            else
            {
                return "";
            }
        }
        public PdfRectangle NextWordBoundingBox(int w, List<Word> words)
        {
            if (w < (words.Count - 1) && words[w].BoundingBox.Left < words[w + 1].BoundingBox.Left)
            {
                return words[w + 1].BoundingBox;
            }
            else
            {
                return new PdfRectangle();
            }
        }
    }


}