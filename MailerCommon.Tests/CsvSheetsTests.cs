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
        actual.Should().HaveCount(2);
        actual[0][0].Should().Be("1");
        actual[0][1].Should().Be("Peter");
        actual[0][2].Should().Be("Parker");
    }

    [Fact]
    public void GivenStartingColumnC_ShouldSkipTwoColumns()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", "Some Sheet!C1:E99");

        actual.Should().NotBeNull();
        actual[0].Should().HaveCount(3);
        actual[0][0].Should().Be("1");
        actual[0][1].Should().Be("Peter");
        actual[0][2].Should().Be("Parker");
    }

    [Fact]
    public void GivenStartingRow3_ShouldSkipTwoRows()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", "Some Sheet!C3:E99");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(2); // rows
        actual[0].Should().HaveCount(3); // columns
        actual[0][0].Should().Be("3");
        actual[0][1].Should().Be("Bruce");
        actual[0][2].Should().Be("Wayne");
    }

    [Fact]
    public void GivenColumnsCtoD_ShouldReturnTwoColumns()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-TwoExtraColumns.csv", "Some Sheet!C1:D99");

        actual.Should().NotBeNull();
        actual[0].Should().HaveCount(2);
        actual[0][0].Should().Be("1");
        actual[0][1].Should().Be("Peter");
    }
}
