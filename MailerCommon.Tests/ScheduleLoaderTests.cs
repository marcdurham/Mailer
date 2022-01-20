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
                "MTG",
                "Test Meeting",
                TimeOnly.Parse("19:30"),
                mondayColumnIndex: 0,
                meetingDateColumnIndex: 0);

            meetings.Should().BeEmpty();
        }

        [Fact]
        public void GivenAssignmentsWithStarts_ShouldReturnStartWithAssignment()
        {
            IList<IList<object>> values = new List<IList<object>>()
            {
                new List<object>() { "Week Date", "Assignment 1 Start", "Assignment 1", "Assignment 2 Start", "Assignment 2" },
                new List<object>() { DateTime.Parse("2022-01-01"), "17:30", "Peter Parker", "17:35", "Clark Kent" },
                new List<object>() { DateTime.Parse("2022-01-07"), "00:00", "Bruce Wayne", "00:10", "Lois Lane" }
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
                "MTG",
                "Test Meeting",
                TimeOnly.Parse("19:30"),
                mondayColumnIndex: 0,
                meetingDateColumnIndex: 0);

            //meetings[0].Assignments["Assignment 1"].Name.Should().Be("Peter Parker");
            meetings[0].Assignments["Assignment 1"].Start.Should().Be(TimeOnly.Parse("17:30"));
            meetings[0].Assignments["Assignment 2"].Start.Should().Be(TimeOnly.Parse("17:35"));
            meetings[1].Assignments["Assignment 1"].Start.Should().Be(TimeOnly.Parse("00:00"));
            meetings[1].Assignments["Assignment 2"].Start.Should().Be(TimeOnly.Parse("00:10"));

            meetings[0].Assignments["Assignment 1"].Friend.Name.Should().Be("Peter Parker");
            meetings[0].Assignments["Assignment 2"].Friend.Name.Should().Be("Clark Kent");
            meetings[1].Assignments["Assignment 1"].Friend.Name.Should().Be("Bruce Wayne");
            meetings[1].Assignments["Assignment 2"].Friend.Name.Should().Be("Lois Lane");
        }

        [Fact]
        public void GivenSectionsWithStarts_ShouldSectionStartTime()
        {
            IList<IList<object>> values = new List<IList<object>>()
            {
                new List<object>() { "Week Date", "Start", "Section A Start", "Section A Assignment 1", "Section A Assignment 2", "Section B Assignment 1" },
                new List<object>() { DateTime.Parse("2011-01-01"), "01:01", "11:11", "Peter Parker", "Clark Kent", "Peter Parker" },
                new List<object>() { DateTime.Parse("2011-01-07"), "02:02", "22:22", "Bruce Wayne", "Lois Lane", "Clark Kent" }
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
                "MTG",
                "Test Meeting",
                TimeOnly.Parse("19:30"),
                mondayColumnIndex: 0,
                meetingDateColumnIndex: 0);

            meetings[0].Assignments["Section A Assignment 1"].Start.Should().Be(TimeOnly.Parse("11:11"));
            meetings[0].Assignments["Section A Assignment 2"].Start.Should().Be(TimeOnly.Parse("11:11"));
            meetings[0].Assignments["Section B Assignment 1"].Start.Should().Be(TimeOnly.Parse("01:01"));
            meetings[1].Assignments["Section A Assignment 1"].Start.Should().Be(TimeOnly.Parse("22:22"));
            meetings[1].Assignments["Section A Assignment 2"].Start.Should().Be(TimeOnly.Parse("22:22"));
            meetings[1].Assignments["Section B Assignment 1"].Start.Should().Be(TimeOnly.Parse("02:02"));
        }
    }
}
