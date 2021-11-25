namespace MailerCommon
{
    public class FriendLoader
    {
        public static Dictionary<string, Friend> GetFriends(IList<IList<object>> rows)
        {
            var friendMap = new Dictionary<string, Friend>();
            foreach (var row in rows)
            {
                var friend = new Friend
                {
                    Key = row[0].ToString().ToUpperInvariant(),
                    Name = row[0].ToString(),
                    PinYinName = row[5].ToString(),
                    SimplifiedChineseName = row[4].ToString(),
                    EmailAddress = row.Count > 6 ? row[6].ToString() : "none",
                };

                friendMap[friend.Key] = friend;
            }

            return friendMap;
        }
    }
}
