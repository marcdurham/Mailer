using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace MailerCommon.Tests;
public class CsvSheetsTests
{
    [Fact]
    public void InvalidRange()
    {
        var sheets = new CsvSheets();
        
        Action action = () => sheets.Read(@".\TestFiles\TestFile1.csv", "Some Sheet!BADRANGE");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InvalidMultiCharacterColumnRange()
    {
        var sheets = new CsvSheets();

        Action action = () => sheets.Read(@".\TestFiles\TestFile1.csv", "Some Sheet!AA33:ZZ44");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MissingSheetName()
    {
        var sheets = new CsvSheets();

        Action action = () => sheets.Read(@".\TestFiles\TestFile1.csv", "A1:E99");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidNoExceptions()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile1.csv", "Some Sheet!A1:C99");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(4);
        actual[0][0].Should().Be("Id");
        actual[0][1].Should().Be("Name");
        actual[0][2].Should().Be("Last Name");
        actual[1][0].Should().Be("1");
        actual[1][1].Should().Be("Peter");
        actual[1][2].Should().Be("Parker");
        actual[2][0].Should().Be("2");
        actual[2][1].Should().Be("Clark");
        actual[2][2].Should().Be("Kent");
    }

    [Fact]
    public void GivenStartingColumnC_ShouldSkipTwoColumns()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", 
            "Some Sheet!C1:E99");

        actual.Should().NotBeNull();
        actual[0].Should().HaveCount(3);
        actual[0][0].Should().Be("Id");
        actual[0][1].Should().Be("Name");
        actual[0][2].Should().Be("Last Name");
        actual[1][0].Should().Be("1");
        actual[1][1].Should().Be("Peter");
        actual[1][2].Should().Be("Parker");
    }

    [Fact]
    public void GivenStartingRow3_ShouldSkipTwoRows()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", 
                "Some Sheet!C3:E99");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(3); // rows
        actual[0].Should().HaveCount(3); // columns
        actual[0][0].Should().Be("2"); // Not a real header
        actual[0][1].Should().Be("Clark");
        actual[0][2].Should().Be("Kent");
        actual[1][0].Should().Be("3");
        actual[1][1].Should().Be("Bruce");
        actual[1][2].Should().Be("Wayne");
    }

    [Fact]
    public void GivenColumnsCtoD_ShouldReturnTwoColumns()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", "Some Sheet!C1:D99");

        actual.Should().NotBeNull();
        actual[0].Should().HaveCount(2);
        actual[0][0].Should().Be("Id");
        actual[0][1].Should().Be("Name");
        actual[1][0].Should().Be("1");
        actual[1][1].Should().Be("Peter");
    }

    [Fact]
    public void GivenEndRow3_ShouldReturnTwoRecordsFromThree()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile1.csv", "Some Sheet!A1:C3");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(3);
        actual[0][0].Should().Be("Id");
        actual[0][1].Should().Be("Name");
        actual[0][2].Should().Be("Last Name");
        actual[1][0].Should().Be("1");
        actual[1][1].Should().Be("Peter");
        actual[1][2].Should().Be("Parker");
        actual[2][0].Should().Be("2");
        actual[2][1].Should().Be("Clark");
        actual[2][2].Should().Be("Kent");
    }

    [Fact]
    public void GiveMultiCharColumns_ShouldBeGreaterThanZ()
    {
        CsvSheets.ColumnIndex("A").Should().Be(0);
        CsvSheets.ColumnIndex("b").Should().Be(1);
        CsvSheets.ColumnIndex("Z").Should().Be(25);
        CsvSheets.ColumnIndex("AA").Should().Be(26);
        CsvSheets.ColumnIndex("AB").Should().Be(27);
        CsvSheets.ColumnIndex("AZ").Should().Be(51);
    }
}
