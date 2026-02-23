using JG.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace JG.Logging.Internal;

/// <summary>
/// High-performance structured logger implementation with allocation-free hot paths.
/// </summary>
internal sealed class StructuredLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogSink[] _sinks;
    private readonly ILogEnricher[] _enrichers;
    private readonly LogLevelFilter _filter;

    public StructuredLogger(
        string categoryName,
        ILogSink[] sinks,
        ILogEnricher[] enrichers,
        LogLevelFilter filter)
    {
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return ScopeContextProvider.CreateScope();
    }

    public bool IsEnabled(LogLevel logLevel) => _sinks.Length > 0 && _filter.IsEnabled(_categoryName, logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        var entry = new LogEntry
        {
            Level = logLevel,
            EventId = eventId,
            Message = message,
            Exception = exception,
            CorrelationId = CorrelationIdProvider.GetCorrelationId(),
            Timestamp = DateTime.UtcNow
        };

        EnrichEntry(entry);
#pragma warning disable CA2012
        WriteAsyncCore(entry).GetAwaiter().GetResult();
#pragma warning restore CA2012
    }

    private void EnrichEntry(LogEntry entry)
    {
        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(entry.Properties);
        }
    }

    private async ValueTask WriteAsyncCore(LogEntry entry)
    {
        foreach (var sink in _sinks)
        {
            await sink.WriteAsync(entry, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
