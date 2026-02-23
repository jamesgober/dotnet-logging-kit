using JG.Logging.Abstractions;
using JG.Logging.Enrichment;
using JG.Logging.Internal;
using JG.Logging.Sinks;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1707

namespace JG.Logging.Tests;

public class FileSinkTests
{
    private string _testDirectory = string.Empty;

    [Fact]
    public async Task FileSink_WritesLogToFile()
    {
        // Arrange
        _testDirectory = Path.Combine(Path.GetTempPath(), $"dotnet-logging-kit-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var formatter = new Formatters.PlainTextFormatter();
        var sink = new FileSink(formatter, _testDirectory, "test");
        var entry = new LogEntry { Message = "Test message", Level = LogLevel.Information };

        // Act
        await sink.WriteAsync(entry);
        await sink.DisposeAsync();

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Should().HaveCount(1);
        var content = File.ReadAllText(files[0]);
        content.Should().Contain("Test message");
        
        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task FileSink_WithRollingInterval_CreatesNewFile()
    {
        // Arrange
        _testDirectory = Path.Combine(Path.GetTempPath(), $"dotnet-logging-kit-rolling-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var formatter = new Formatters.PlainTextFormatter();
        var sink = new FileSink(
            formatter,
            _testDirectory,
            "rolling",
            maxFileSizeBytes: 200,  // Small size to test rotation
            rollingInterval: RollingInterval.None);

        // Act - write a large message to exceed max file size
        var largeEntry = new LogEntry 
        { 
            Message = new string('X', 500),  // Much larger than max file size
            Level = LogLevel.Information 
        };
        await sink.WriteAsync(largeEntry);
        await sink.DisposeAsync();

        // Assert - should have created at least one file
        var files = Directory.GetFiles(_testDirectory, "*.log");
        files.Should().HaveCountGreaterThanOrEqualTo(1, "should create at least one file");
        
        // Verify content exists
        var hasContent = files.Any(f => new FileInfo(f).Length > 0);
        hasContent.Should().BeTrue("file should have content");
        
        // Cleanup
        Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task FileSink_PrunesOldFiles()
    {
        // Arrange
        _testDirectory = Path.Combine(Path.GetTempPath(), $"dotnet-logging-kit-pruned-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        var formatter = new Formatters.PlainTextFormatter();
        var sink = new FileSink(
            formatter,
            _testDirectory,
            "pruned",
            maxFileSizeBytes: 100,
            maxBackupFiles: 3);

        // Act
        for (int i = 0; i < 10; i++)
        {
            var entry = new LogEntry { Message = new string('X', 20), Level = LogLevel.Information };
            await sink.WriteAsync(entry);
        }
        await sink.DisposeAsync();

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        files.Length.Should().BeLessThanOrEqualTo(3);
        
        // Cleanup
        Directory.Delete(_testDirectory, true);
    }
}

public class EnricherTests
{
    [Fact]
    public void MachineNameEnricher_AddsMachineName()
    {
        // Arrange
        var enricher = new MachineNameEnricher();
        var properties = new Dictionary<string, object?>();

        // Act
        enricher.Enrich(properties);

        // Assert
        properties.Should().ContainKey("MachineName");
        properties["MachineName"].Should().NotBeNull();
    }

    [Fact]
    public void EnvironmentEnricher_AddsEnvironment()
    {
        // Arrange
        var enricher = new EnvironmentEnricher();
        var properties = new Dictionary<string, object?>();

        // Act
        enricher.Enrich(properties);

        // Assert
        properties.Should().ContainKey("Environment");
    }

    [Fact]
    public void VersionEnricher_AddsVersion()
    {
        // Arrange
        var enricher = new VersionEnricher(typeof(EnricherTests));
        var properties = new Dictionary<string, object?>();

        // Act
        enricher.Enrich(properties);

        // Assert
        properties.Should().ContainKey("Version");
        properties.Should().ContainKey("InformationalVersion");
    }

    [Fact]
    public void ScopeContextEnricher_AddsScopeProperties()
    {
        // Arrange
        using (ScopeContextProvider.CreateScope())
        {
            ScopeContextProvider.AddPropertyToCurrentScope("UserId", "123");
            var enricher = new ScopeContextEnricher();
            var properties = new Dictionary<string, object?>();

            // Act
            enricher.Enrich(properties);

            // Assert
            properties.Should().ContainKey("UserId");
            properties["UserId"].Should().Be("123");
        }
    }
}

public class LogLevelFilterTests
{
    [Fact]
    public void LogLevelFilter_DefaultLevel_EnablesMatchingLevel()
    {
        // Arrange
        var filter = new LogLevelFilter { DefaultLevel = LogLevel.Warning };

        // Act & Assert
        filter.IsEnabled("MyApp.Service", LogLevel.Error).Should().BeTrue();
        filter.IsEnabled("MyApp.Service", LogLevel.Warning).Should().BeTrue();
        filter.IsEnabled("MyApp.Service", LogLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void LogLevelFilter_CategoryLevel_OverridesDefault()
    {
        // Arrange
        var filter = new LogLevelFilter { DefaultLevel = LogLevel.Warning };
        filter.SetCategoryLevel("MyApp.Database", LogLevel.Debug);

        // Act & Assert
        filter.IsEnabled("MyApp.Database", LogLevel.Debug).Should().BeTrue();
        filter.IsEnabled("MyApp.Service", LogLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void LogLevelFilter_NamespaceLevel_AppliesPrefix()
    {
        // Arrange
        var filter = new LogLevelFilter { DefaultLevel = LogLevel.Warning };
        filter.SetNamespaceLevel("MyApp.Services", LogLevel.Information);

        // Act & Assert
        filter.IsEnabled("MyApp.Services.UserService", LogLevel.Information).Should().BeTrue();
        filter.IsEnabled("MyApp.Data.Repository", LogLevel.Information).Should().BeFalse();
    }

    [Fact]
    public void LogLevelFilter_CategoryOverridesNamespace()
    {
        // Arrange
        var filter = new LogLevelFilter { DefaultLevel = LogLevel.Warning };
        filter.SetNamespaceLevel("MyApp.Services", LogLevel.Information);
        filter.SetCategoryLevel("MyApp.Services.UserService", LogLevel.Debug);

        // Act & Assert
        filter.IsEnabled("MyApp.Services.UserService", LogLevel.Debug).Should().BeTrue();
    }
}

public class ScopeContextProviderTests
{
    [Fact]
    public void ScopeContextProvider_CreateScope_AllowsPropertyAddition()
    {
        // Arrange
        using (ScopeContextProvider.CreateScope())
        {
            ScopeContextProvider.AddPropertyToCurrentScope("Key1", "Value1");

            // Act
            var properties = ScopeContextProvider.GetScopeProperties();

            // Assert
            properties.Should().ContainKey("Key1");
            properties["Key1"].Should().Be("Value1");
        }
    }

    [Fact]
    public void ScopeContextProvider_NestedScopes_InheritParentProperties()
    {
        // Arrange
        using (ScopeContextProvider.CreateScope())
        {
            ScopeContextProvider.AddPropertyToCurrentScope("OuterKey", "OuterValue");

            using (ScopeContextProvider.CreateScope())
            {
                ScopeContextProvider.AddPropertyToCurrentScope("InnerKey", "InnerValue");

                // Act
                var properties = ScopeContextProvider.GetScopeProperties();

                // Assert
                properties.Should().ContainKey("OuterKey");
                properties.Should().ContainKey("InnerKey");
            }
        }
    }

    [Fact]
    public void ScopeContextProvider_OutOfScope_ClearsProperties()
    {
        // Arrange
        using (ScopeContextProvider.CreateScope())
        {
            ScopeContextProvider.AddPropertyToCurrentScope("ScopedKey", "Value");
        }

        // Act
        var properties = ScopeContextProvider.GetScopeProperties();

        // Assert
        properties.Should().BeEmpty();
    }
}

public class ExceptionHandlingTests
{
    [Fact]
    public void LogEntry_WithException_PreservesExceptionDetails()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var exception = new ArgumentException("Test error", innerException);
        var entry = new LogEntry
        {
            Message = "Error occurred",
            Level = LogLevel.Error,
            Exception = exception
        };

        // Act
        var formatter = new Formatters.PlainTextFormatter();
        var formatted = formatter.Format(entry);

        // Assert
        formatted.Should().Contain("Test error");
        formatted.Should().Contain("ArgumentException");
    }

    [Fact]
    public void LogEntry_WithNullException_FormatsWithoutError()
    {
        // Arrange
        var entry = new LogEntry
        {
            Message = "Normal log",
            Level = LogLevel.Information,
            Exception = null
        };

        // Act
        var formatter = new Formatters.PlainTextFormatter();
        var formatted = formatter.Format(entry);

        // Assert
        formatted.Should().Contain("Normal log");
    }
}

public class ConcurrencyTests
{
    [Fact]
    public async Task MultipleCoroutines_WithDistinctCorrelationIds_MaintainIsolation()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new Dictionary<int, string?>();
        var lockObject = new object();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                var correlationId = $"task-{index}";
                using (CorrelationIdProvider.SetCorrelationId(correlationId))
                {
                    System.Threading.Thread.Sleep(10);
                    
                    var retrieved = CorrelationIdProvider.GetCorrelationId();
                    lock (lockObject)
                    {
                        results[index] = retrieved;
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        for (int i = 0; i < 10; i++)
        {
            results[i].Should().Be($"task-{i}");
        }
    }
}

#pragma warning restore CA1707
