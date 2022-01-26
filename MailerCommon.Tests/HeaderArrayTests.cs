using Xunit;

namespace MailerCommon.Tests
{
    public class HeaderArrayTests
    {
        string[] _headers = {
                "Start",
                "Section A Start",
                "Section A Assignment 1 Start",
                "Section A Assignment 1 "
            };

        [Theory]
        [InlineData("Section A Assignment 1", 2)]
        [InlineData("Section A Assignment X", 1)]
        [InlineData("Section X Assignment X", 0)]
        [InlineData("Another Assignment", 0)]
        public void GivenAssignmentColumn_ShouldReturnStartColumnIndex(string assignmentColumnHeader, int expectedStartColumnIndex)
        {
            int actual = HeaderArray.StartColumnIndexOf(_headers, assignmentColumnHeader);

            Assert.Equal(expectedStartColumnIndex, actual);
        }
    }
}
