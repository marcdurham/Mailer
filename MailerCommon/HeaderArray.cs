namespace MailerCommon
{
    public class HeaderArray
    {
        public static int StartColumnIndexOf(string[] headers, string headerColumn)
        {
            string[] headerWords = headerColumn.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            for (int i = (headerWords.Length); i >= 0; i--)
            {
                string[] shortened = new string[i];
                for (int j = 0; j < shortened.Length; j++)
                    shortened[j] = headerWords[j];
                
                string shortenedHeader = $"{string.Join(' ', shortened)} Start".Trim();
                int index = headers.ToList().IndexOf(shortenedHeader);
                if (index >= 0)
                    return index;
            }

            return -1;
        }
    }
}
