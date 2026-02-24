using JG.Logging.Abstractions;
using JG.Logging.Formatters;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591
#pragma warning disable CA1707

namespace JG.Logging.Tests;

public class PlainTextFormatterTests
{
    [Fact]
    public void Format_WithValidEntry_ReturnsFormattedString()
    {
        // Arrange
        var formatter = new PlainTextFormatter();
        var entry = new LogEntry
        {
            Message = "Test message",
            Level = Microsoft.Extensions.Logging.LogLevel.Information
        };

        // Act
        var result = formatter.Format(entry);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Test message");
        result.Should().Contain("INFORMATION");
    }

    [Fact]
    public void Format_WithNullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var formatter = new PlainTextFormatter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => formatter.Format(null!));
    }

    [Fact]
    public void Format_WithCorrelationId_IncludesCorrelationId()
    {
        // Arrange
        var formatter = new PlainTextFormatter();
        var entry = new LogEntry
        {
            Message = "Test",
            CorrelationId = "test-correlation-123",
            Level = Microsoft.Extensions.Logging.LogLevel.Error
        };

        // Act
        var result = formatter.Format(entry);

        // Assert
        result.Should().Contain("test-correlation-123");
        result.Should().Contain("CorrelationId");
    }
}

#pragma warning restore CA1707
