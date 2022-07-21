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
                    foreach(Line lin in lineObjects)
                    {
                        builder.AppendLine($"{lineObjects.IndexOf(lin)}:: {lin.AllText()}");
                        if(IsWeekHeader(lin) && lin.ContainsBoxText("|"))
                        {
                            weekHeader = lin.AllText().Split('|')[0];
                        }
                        foreach(var box in lin.Boxes)
                        {
                            builder.AppendLine(box.Text);
                        }

                        var assignments = Process(lin, weekHeader);

                        foreach(Assignment assignment in assignments)
                        {
                            builder.AppendLine($"ASSIGNMENT: WK: {assignment.WeekHeader} ST: {assignment.Start} MIN: {assignment.Minutes} AN: {assignment.AssignmentName}");
                        }
                        builder.AppendLine();
                    }

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

        List<Assignment> Process(Line line, string weekHeader)
        {
            if(line.Boxes.Count > 3 && Regex.IsMatch(line.Boxes[0].Text, @"^\d+:\d\d$"))
            {
                string assignmentName = line.Boxes[2].Text;
                int minutes = 0;
                //if(line.Boxes[3].Text == "分鐘）" && Regex.IsMatch(line.Boxes[2].Text, @"(.*)（\d+"))
                //{
                //    assignmentName = Regex.Replace(line.Boxes[2].Text, @"（\d+$", "");
                //    string minutesText = line.Boxes[2].Text.Split('（').Last();
                //    minutes = int.Parse(minutesText);
                //}

                var minutesPattern = new Regex(@"\d+:\d\d•(.*)[（(](\d+)分鐘[）)]");
                var match = minutesPattern.Match(line.AllText());
                if(match.Success)
                {
                    string minutePart = match.Groups[2].Value;
                    minutes = int.Parse(minutePart);
                    assignmentName = match.Groups[1].Value;
                }

                var assignment = new Assignment
                {
                    WeekHeader = weekHeader,
                    Start = TimeOnly.Parse(line.Boxes[0].Text),
                    AssignmentName = assignmentName,
                    //AssigneeName = line.Boxes[2].Text,
                    Minutes = minutes
                };

                return new List<Assignment> { assignment };
            }

            return new List<Assignment>();
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