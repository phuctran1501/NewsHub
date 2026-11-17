namespace NewsHub.Domain.Tests.Common;

using NewsHub.Domain.Common;
using Xunit;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Thời sự", "thoi-su")]
    [InlineData("Công nghệ", "cong-nghe")]
    [InlineData("Kinh doanh", "kinh-doanh")]
    [InlineData("Khoa học & Đời sống", "khoa-hoc-doi-song")]
    [InlineData("Thế giới - Quân sự!!", "the-gioi-quan-su")]
    [InlineData("Đồng bằng sông Cửu Long", "dong-bang-song-cuu-long")]
    [InlineData("   Xe   và   Giao thông   ", "xe-va-giao-thong")]
    [InlineData("AI & Machine Learning (2026)", "ai-machine-learning-2026")]
    [InlineData("Trí tuệ nhân tạo --- Bán dẫn", "tri-tue-nhan-tao-ban-dan")]
    public void GenerateSlug_WithVietnameseText_ShouldGenerateValidKebabCaseSlug(string input, string expected)
    {
        // Act
        var result = SlugHelper.GenerateSlug(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("----")]
    [InlineData("!@#$%^&*()_+")]
    public void GenerateSlug_WithEmptyOrSpecialCharsOnly_ShouldReturnEmptyString(string? input)
    {
        // Act
        var result = SlugHelper.GenerateSlug(input);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GenerateSlug_WithLongText_ShouldTruncateToMax120Chars()
    {
        // Arrange
        var input = new string('a', 150);

        // Act
        var result = SlugHelper.GenerateSlug(input);

        // Assert
        Assert.Equal(120, result.Length);
        Assert.DoesNotContain("--", result);
        Assert.False(result.EndsWith('-'));
    }
}
