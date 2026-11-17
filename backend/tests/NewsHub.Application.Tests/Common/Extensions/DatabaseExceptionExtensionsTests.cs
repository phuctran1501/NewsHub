namespace NewsHub.Application.Tests.Common.Extensions;

using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using Xunit;

public class DatabaseExceptionExtensionsTests
{
    [Fact]
    public void IsUniqueConstraintViolation_WhenConcurrencyException_ShouldReturnFalse()
    {
        var ex = new DbUpdateConcurrencyException("Concurrency conflict");
        Assert.False(ex.IsUniqueConstraintViolation());
    }

    [Fact]
    public void IsForeignKeyViolation_WhenConcurrencyException_ShouldReturnFalse()
    {
        var ex = new DbUpdateConcurrencyException("Concurrency conflict");
        Assert.False(ex.IsForeignKeyViolation());
    }

    [Theory]
    [InlineData("23505: duplicate key value violates unique constraint")]
    [InlineData("Violation of UNIQUE KEY constraint 'UQ_Name'")]
    [InlineData("UNIQUE constraint failed: categories.name")]
    [InlineData("Cannot insert duplicate key in object")]
    public void IsUniqueConstraintViolation_WhenMessageMatchesUniquePattern_ShouldReturnTrue(string message)
    {
        var ex = new DbUpdateException("Database error", new Exception(message));
        Assert.True(ex.IsUniqueConstraintViolation());
    }

    [Theory]
    [InlineData("23503: insert or update on table violates foreign key constraint")]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint")]
    [InlineData("FOREIGN KEY constraint failed")]
    [InlineData("violates foreign key constraint 'fk_article_category'")]
    public void IsForeignKeyViolation_WhenMessageMatchesForeignKeyPattern_ShouldReturnTrue(string message)
    {
        var ex = new DbUpdateException("Database error", new Exception(message));
        Assert.True(ex.IsForeignKeyViolation());
    }

    [Fact]
    public void IsUniqueConstraintViolation_WhenUnrelatedError_ShouldReturnFalse()
    {
        var ex = new DbUpdateException("Database error", new Exception("Connection timeout expired."));
        Assert.False(ex.IsUniqueConstraintViolation());
    }

    [Fact]
    public void IsForeignKeyViolation_WhenUnrelatedError_ShouldReturnFalse()
    {
        var ex = new DbUpdateException("Database error", new Exception("Disk full or connection aborted."));
        Assert.False(ex.IsForeignKeyViolation());
    }
}
