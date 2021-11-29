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
    public void MissingSheetName()
    {
        var sheets = new CsvSheets();

        Action action = () => sheets.Read(@".\TestFiles\TestFile1.csv", "A1:Z99");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidNoExceptions()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile1.csv", "Some Sheet!C3:Z99");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(2);
        actual[0][0].Should().Be("1");
        actual[0][1].Should().Be("Peter");
        actual[0][2].Should().Be("Parker");
    }

    [Fact]
    public void GivenExtraColumn_ShouldSkipIt()
    {
        var sheets = new CsvSheets();

        IList<IList<object>> actual = sheets
            .Read(@".\TestFiles\TestFile2-ExtraColumns.csv", "Some Sheet!C1:Z99");

        actual.Should().NotBeNull();
        actual.Should().HaveCount(2);
        actual[0][0].Should().Be("1");
        actual[0][1].Should().Be("Peter");
        actual[0][2].Should().Be("Parker");
    }
}
