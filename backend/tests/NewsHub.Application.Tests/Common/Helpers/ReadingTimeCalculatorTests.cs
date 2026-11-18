namespace NewsHub.Application.Tests.Common.Helpers;

using NewsHub.Application.Common.Helpers;
using Xunit;

public class ReadingTimeCalculatorTests
{
    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("   ", 1)]
    [InlineData("<p></p>", 1)]
    public void Calculate_WithEmptyOrWhitespace_ShouldReturnOneMinute(string? content, int expected)
    {
        var result = ReadingTimeCalculator.Calculate(content);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_WithLessThan200Words_ShouldReturnOneMinute()
    {
        var content = string.Join(" ", Enumerable.Repeat("từ", 50));
        var result = ReadingTimeCalculator.Calculate(content);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Calculate_WithExactly200Words_ShouldReturnOneMinute()
    {
        var content = string.Join(" ", Enumerable.Repeat("từ", 200));
        var result = ReadingTimeCalculator.Calculate(content);
        Assert.Equal(1, result);
    }

    [Fact]
    public void Calculate_With201Words_ShouldCeilToTwoMinutes()
    {
        var content = string.Join(" ", Enumerable.Repeat("từ", 201));
        var result = ReadingTimeCalculator.Calculate(content);
        Assert.Equal(2, result);
    }

    [Fact]
    public void Calculate_WithHtmlTags_ShouldStripTagsAndCountOnlyWords()
    {
        var content = "<div><p>" + string.Join(" ", Enumerable.Repeat("báo", 400)) + "</p><a href='#'>link</a></div>";
        var result = ReadingTimeCalculator.Calculate(content);
        // 400 words + 1 word ('link') = 401 words -> ceil(401 / 200) = 3 minutes
        Assert.Equal(3, result);
    }
}
