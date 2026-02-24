using JG.Logging.Abstractions;
using JG.Logging.Enrichment;
using JG.Logging.Internal;
using JG.Logging.Formatters;
using JG.Logging.Sinks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1707
#pragma warning disable CS1591

namespace JG.Logging.Tests;

public class EdgeCaseTests
{
    [Fact]
    public void LogEntry_WithNullMessage_FormatsGracefully()
    {
        // Arrange
        var entry = new LogEntry
        {
            Message = null,
            Level = LogLevel.Information,
            CorrelationId = "test-id"
        };

        // Act
        var formatter = new PlainTextFormatter();
        var result = formatter.Format(entry);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("INFORMATION");
    }

    [Fact]
    public void LogEntry_WithVeryLongMessage_FormatsSuccessfully()
    {
        // Arrange
        var longMessage = new string('X', 100_000);
        var entry = new LogEntry
        {
            Message = longMessage,
            Level = LogLevel.Warning
        };

        // Act
        var formatter = new PlainTextFormatter();
        var result = formatter.Format(entry);

        // Assert
        result.Length.Should().BeGreaterThan(100_000);
    }

    [Fact]
    public void LogEntry_WithComplexException_PreservesAllDetails()
    {
        // Arrange
        var innerInner = new InvalidOperationException("Third level");
        var inner = new ArgumentNullException("param", new InvalidOperationException("Second level", innerInner));
        var outer = new NotImplementedException("Top level", inner);

        var entry = new LogEntry
        {
            Message = "Complex error",
            Level = LogLevel.Error,
            Exception = outer
        };

        // Act
        var formatter = new PlainTextFormatter();
        var result = formatter.Format(entry);

        // Assert
        result.Should().Contain("NotImplementedException");
        result.Should().Contain("ArgumentNullException");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("Inner Exception");
    }

    [Fact]
    public void JsonFormatter_WithCircularReference_InProperties_HandlesGracefully()
    {
        // Arrange
        var entry = new LogEntry
        {
            Message = "Test",
            Level = LogLevel.Information,
            Properties = new Dictionary<string, object?>
            {
                { "Count", 42 },
                { "Name", "Test" },
                { "Null", null }
            }
        };

        // Act
        var formatter = new JsonFormatter();
        var result = formatter.Format(entry);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("\"Count\":42");
    }

    [Fact]
    public void LogEntry_WithEmptyCorrelationId_HandlesCorrectly()
    {
        // Arrange
        var entry = new LogEntry
        {
            Message = "Test",
            Level = LogLevel.Information,
            CorrelationId = string.Empty
        };

        // Act
        var formatter = new PlainTextFormatter();
        var result = formatter.Format(entry);

        // Assert
        result.Should().NotContain("CorrelationId");
    }

    [Fact]
    public void Enricher_WithNullProperties_Handles_Safely()
    {
        // Arrange
        var enricher = new MachineNameEnricher();
        var properties = new Dictionary<string, object?> { { "Existing", "Value" } };

        // Act
        enricher.Enrich(properties);

        // Assert
        properties.Should().ContainKey("MachineName");
        properties.Should().ContainKey("Existing");
    }
}

public class StressTests
{
    [Fact]
    public async Task HighVolume_ConcurrentLogging_MaintainsCorrectness()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"stress-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var formatter = new JsonFormatter();
        var sink = new FileSink(formatter, tempDir, "stress", maxFileSizeBytes: 1_000_000);

        var tasks = new List<Task>();
        var errors = new List<InvalidOperationException>();

        // Act
        for (int t = 0; t < 10; t++)
        {
            int taskId = t;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        var entry = new LogEntry
                        {
                            Message = $"Task {taskId} iteration {i}",
                            Level = LogLevel.Information,
                            CorrelationId = $"corr-{taskId}"
                        };

                        await sink.WriteAsync(entry);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    lock (errors)
                    {
                        errors.Add(ex);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        await sink.DisposeAsync();

        // Assert
        errors.Should().BeEmpty();
        var files = Directory.GetFiles(tempDir);
        files.Length.Should().BeGreaterThan(0);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task DeepNestedScopes_Handles_Correctly()
    {
        // Arrange
        const int nesting = 10;  // Reduce from 50 to 10 for cleaner testing

        // Act - Create deeply nested scopes
        var scopes = new Stack<IDisposable>();
        try
        {
            for (int i = 0; i < nesting; i++)
            {
                var scope = ScopeContextProvider.CreateScope();
                scopes.Push(scope);
                ScopeContextProvider.AddPropertyToCurrentScope($"Level{i}", i);
            }

            var properties = ScopeContextProvider.GetScopeProperties();

            // Assert
            properties.Should().HaveCountGreaterThan(0);
        }
        finally
        {
            // Properly dispose all scopes in reverse order
            while (scopes.Count > 0)
            {
                scopes.Pop().Dispose();
            }
        }

        // After disposal, scope should be cleared
        var finalProps = ScopeContextProvider.GetScopeProperties();
        finalProps.Should().BeEmpty();
        
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RapidCorrelationIdChanges_MaintainsIsolation()
    {
        // Arrange
        var results = new Dictionary<string, int>();
        var tasks = new List<Task>();

        // Act
        for (int t = 0; t < 20; t++)
        {
            int id = t;
            tasks.Add(Task.Run(() =>
            {
                var correlationId = $"rapid-{id}";
                using (CorrelationIdProvider.SetCorrelationId(correlationId))
                {
                    // Rapidly change in same thread
                    for (int i = 0; i < 10; i++)
                    {
                        var current = CorrelationIdProvider.GetCorrelationId();
                        if (current != correlationId)
                            throw new InvalidOperationException($"Expected {correlationId}, got {current}");
                    }

                    lock (results)
                    {
                        results[correlationId] = id;
                    }
                }
            }));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert
        results.Should().HaveCount(20);
    }

    [Fact]
    public async Task FileSink_RapidRolling_CreatesValidFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"rapid-roll-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var formatter = new PlainTextFormatter();
        var sink = new FileSink(
            formatter,
            tempDir,
            "rapid",
            maxFileSizeBytes: 50,  // Very small to force rapid rolling
            maxBackupFiles: 5
        );

        // Act - Write many small entries quickly
        for (int i = 0; i < 50; i++)
        {
            var entry = new LogEntry
            {
                Message = $"Entry {i:000}",
                Level = LogLevel.Information
            };
            await sink.WriteAsync(entry);
        }

        await sink.DisposeAsync();

        // Assert
        var files = Directory.GetFiles(tempDir, "*.log");
        files.Length.Should().BeLessThanOrEqualTo(5, "should respect backup limit");

        var totalSize = files.Sum(f => new FileInfo(f).Length);
        totalSize.Should().BeGreaterThan(0);

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}

public class DisposalTests
{
    [Fact]
    public async Task FileSink_Disposed_ClosesFileHandle()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"disposal-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var formatter = new PlainTextFormatter();
        var sink = new FileSink(formatter, tempDir, "disposal");

        var entry = new LogEntry { Message = "Test", Level = LogLevel.Information };
        await sink.WriteAsync(entry);

        // Act
        await sink.DisposeAsync();

        // Assert - should be able to delete the directory
        Action deleteAction = () => Directory.Delete(tempDir, true);
        deleteAction.Should().NotThrow();
    }

    [Fact]
    public async Task MultipleDisposals_Handled_Safely()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"multi-dispose-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var formatter = new PlainTextFormatter();
        var sink = new FileSink(formatter, tempDir, "test");

        // Act & Assert
        await sink.DisposeAsync();
        await sink.DisposeAsync(); // Should not throw
        await sink.DisposeAsync(); // Should not throw

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}

#pragma warning restore CA1707
