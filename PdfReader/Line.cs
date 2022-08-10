namespace PdfReader;

public class Line
{
    public List<Box> Boxes { get; set; } = new();
    public string AllText()
    {
        return string.Join(string.Empty, Boxes.Select(b => b.Text).ToList());
    }

    public bool ContainsBoxText(string text)
    {
        return Boxes.Select(b => b.Text).ToList().Contains(text);
    }
}
