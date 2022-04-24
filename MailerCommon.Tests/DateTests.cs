using Mailer.Sender;
using System;
using Xunit;

namespace MailerCommon.Tests
{
    public class DateTests
    {
        [Theory]
        [InlineData("2022-04-29", "2022-04-25")] // 2022-04-29 is Friday
        [InlineData("2022-04-28", "2022-04-25")] // 2022-04-28 is Thursday
        [InlineData("2022-04-27", "2022-04-25")] // 2022-04-27 is Wednesday
        [InlineData("2022-04-26", "2022-04-25")] // 2022-04-26 is Tuesday
        [InlineData("2022-04-25", "2022-04-25")] // 2022-04-25 is Monday
        [InlineData("2022-04-24", "2022-04-18")] // 2022-04-24 is Sunday
        [InlineData("2022-04-23", "2022-04-18")] // 2022-04-23 is Saturday
        [InlineData("2022-04-22", "2022-04-18")] // 2022-04-22 is Friday
        [InlineData("2022-04-21", "2022-04-18")] // 2022-04-21 is Thursday
        [InlineData("2022-04-20", "2022-04-18")] // 2022-04-20 is Wednesday
        [InlineData("2022-04-19", "2022-04-18")] // 2022-04-19 is Tuesday
        [InlineData("2022-04-18", "2022-04-18")] // 2022-04-18 is Monday
        public void GivenMonday_ShouldBeMonday(string today, string expectedMonday)
        {
            Assert.Equal(DateTime.Parse(expectedMonday), PublisherEmailer.GetMonday(DateTime.Parse(today)));
        }
    }
}
