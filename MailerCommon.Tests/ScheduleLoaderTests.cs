using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MailerCommon.Tests
{
    public class ScheduleLoaderTests
    {
        [Fact]
        public void GivenEmptyValues_ShouldReturnEmptyList()
        {
            IList<IList<object>> values = new List<IList<object>>();
            Dictionary<string, Friend> friendMap = new Dictionary<string, Friend>()
            {
                { "JOHN DOE", new Friend(){ } },
                { "PETER PARKER", new Friend(){ } }
            };

            List<Meeting> meetings = ScheduleLoader.GetSchedule(
                values,
                friendMap,
                new int[] { 4 },
                "Test Meeting",
                TimeOnly.Parse("19:30"),
                mondayColumnIndex: 0,
                meetingDateColumnIndex: 0);

            meetings.Should().BeEmpty();
        }

        [Fact]
        public void GivenSomeValues_ShouldReturnSomething()
        {
            IList<IList<object>> values = new List<IList<object>>()
            {
                new List<object>() { "Week Date", "Assignment 1", "Assignment 2" },
                new List<object>() { DateTime.Parse("2022-01-01"), "Peter Parker", "Clark Kent" },
                new List<object>() { DateTime.Parse("2022-01-07"), "Bruce Wayne", "Lois Lane" }
            };

            Dictionary<string, Friend> friendMap = new Dictionary<string, Friend>()
            {
                { "CLARK KENT", new Friend(){ Name = "Clark Kent" } },
                { "PETER PARKER", new Friend(){ Name = "Peter Parker" } },
                { "BRUCE WAYNE", new Friend(){ Name = "Bruce Wayne" } },
                { "LOIS LANE", new Friend(){ Name = "Lois Lane" } }
            };

            List<Meeting> meetings = ScheduleLoader.GetSchedule(
                values,
                friendMap,
                new int[] { 4 },
                "Test Meeting",
                TimeOnly.Parse("19:30"),
                mondayColumnIndex: 0,
                meetingDateColumnIndex: 0);

            //meetings[0].Assignments["Assignment 1"].Name.Should().Be("Peter Parker");
            meetings[0].Assignments["Assignment 1"].Friend.Name.Should().Be("Peter Parker");
            meetings[0].Assignments["Assignment 2"].Friend.Name.Should().Be("Clark Kent");
            meetings[1].Assignments["Assignment 1"].Friend.Name.Should().Be("Bruce Wayne");
            meetings[1].Assignments["Assignment 2"].Friend.Name.Should().Be("Lois Lane");
        }
    }
}
