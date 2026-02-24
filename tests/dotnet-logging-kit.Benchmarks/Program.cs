using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using JG.Logging.Abstractions;
using JG.Logging.Formatters;
using JG.Logging.Sinks;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1591

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var summary = BenchmarkRunner.Run<LoggingBenchmarks>();

[SimpleJob]
[MemoryDiagnoser]
public class LoggingBenchmarks
{
    private ConsoleSink _consoleSink = null!;
    private FileSink _fileSink = null!;

    [GlobalSetup]
    public void Setup()
    {
        var plainFormatter = new PlainTextFormatter();
        var jsonFormatter = new JsonFormatter();

        _consoleSink = new ConsoleSink(plainFormatter);
        var testDir = Path.Combine(Path.GetTempPath(), "benchmark-logs");
        Directory.CreateDirectory(testDir);
        _fileSink = new FileSink(jsonFormatter, testDir);
    }

    [Benchmark]
    public async Task LogToConsole()
    {
        var entry = new LogEntry
        {
            Message = "Benchmark log entry with some data",
            Level = LogLevel.Information,
            CorrelationId = "bench-123"
        };

        await _consoleSink.WriteAsync(entry);
    }

    [Benchmark]
    public async Task LogToFile()
    {
        var entry = new LogEntry
        {
            Message = "Benchmark log entry with some data",
            Level = LogLevel.Information,
            CorrelationId = "bench-123"
        };

        await _fileSink.WriteAsync(entry);
    }

    [Benchmark]
    public void PlainTextFormatter()
    {
        var formatter = new PlainTextFormatter();
        var entry = new LogEntry
        {
            Message = "Test message",
            Level = LogLevel.Warning,
            CorrelationId = "test-id",
            Exception = new InvalidOperationException("Test error")
        };

        _ = formatter.Format(entry);
    }

    [Benchmark]
    public void JsonFormatter()
    {
        var formatter = new JsonFormatter();
        var entry = new LogEntry
        {
            Message = "Test message",
            Level = LogLevel.Warning,
            CorrelationId = "test-id"
        };

        _ = formatter.Format(entry);
    }

    [GlobalCleanup]
    public async ValueTask Cleanup()
    {
        await _consoleSink.DisposeAsync();
        await _fileSink.DisposeAsync();
    }
}
