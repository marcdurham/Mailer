using Mailer.Sender;

namespace MailerCommon
{
    public class EmailRecipientLoader
    {
        public static List<EmailRecipient> ConvertToEmailRecipients(IList<IList<object>> rows)
        {
            var tasks = new List<EmailRecipient>();
            foreach (IList<object> row in rows)
            {
                if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0].ToString()))
                    break; // End of list

                string? email = row.Count > 1 ? $"{row[1]}" : null;
                string? sent = row.Count > 2 ? $"{row[2]}" : null;
                string? sentStatus = row.Count > 3 ? $"{row[3]}" : null;
                string? check = row.Count > 4 ? $"{row[4]}" : null;
                string? checkStatus = row.Count > 5 ? $"{row[5]}" : null;
                tasks.Add(
                    new EmailRecipient
                    {
                        Name = $"{row[0]}",
                        EmailAddress = email, // TODO: Get email address from somewhere else?
                        Sent = $"{sent}",
                        SentStatus = $"{sentStatus}",
                        Check = $"{check}",
                        CheckStatus = $"{checkStatus}"
                    });
            }

            return tasks;
        }
    }
}
