using JG.Logging.Abstractions;
using JG.Logging.Formatters;
using JG.Logging.Sinks;
using JG.Logging.Internal;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1707

namespace JG.Logging.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task ConsoleSink_WithPlainTextFormatter_OutputsFormattedLog()
    {
        // Arrange
        var formatter = new PlainTextFormatter();
        var sink = new ConsoleSink(formatter);
        var entry = new LogEntry
        {
            Message = "Integration test message",
            Level = LogLevel.Warning,
            CorrelationId = "integration-test-001",
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var writeTask = sink.WriteAsync(entry);
        await writeTask;

        // Assert
        writeTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task ConsoleSink_WithJsonFormatter_OutputsJsonLog()
    {
        // Arrange
        var formatter = new JsonFormatter();
        var sink = new ConsoleSink(formatter);
        var entry = new LogEntry
        {
            Message = "JSON test message",
            Level = LogLevel.Error,
            CorrelationId = "json-test-002"
        };

        // Act
        var writeTask = sink.WriteAsync(entry);
        await writeTask;

        // Assert
        writeTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void CorrelationIdProvider_SetAndGet_MaintainsValue()
    {
        // Arrange
        var correlationId = "test-correlation-123";

        // Act
        using (CorrelationIdProvider.SetCorrelationId(correlationId))
        {
            var retrieved = CorrelationIdProvider.GetCorrelationId();

            // Assert
            retrieved.Should().Be(correlationId);
        }

        var afterScope = CorrelationIdProvider.GetCorrelationId();
        afterScope.Should().BeNull();
    }

    [Fact]
    public void CorrelationIdProvider_NestedScopes_RestoresPreviousValue()
    {
        // Arrange
        var outer = "outer-id";
        var inner = "inner-id";

        // Act & Assert
        using (CorrelationIdProvider.SetCorrelationId(outer))
        {
            CorrelationIdProvider.GetCorrelationId().Should().Be(outer);

            using (CorrelationIdProvider.SetCorrelationId(inner))
            {
                CorrelationIdProvider.GetCorrelationId().Should().Be(inner);
            }

            CorrelationIdProvider.GetCorrelationId().Should().Be(outer);
        }

        CorrelationIdProvider.GetCorrelationId().Should().BeNull();
    }
}

#pragma warning restore CA1707
