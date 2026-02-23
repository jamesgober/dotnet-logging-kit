using JG.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace JG.Logging.Internal;

/// <summary>
/// Logger provider that creates instances of StructuredLogger with configured sinks and enrichers.
/// </summary>
internal sealed class StructuredLoggerProvider : ILoggerProvider
{
    private readonly ILogSink[] _sinks;
    private readonly ILogEnricher[] _enrichers;
    private readonly LogLevelFilter _filter;
    private bool _disposed;

    public StructuredLoggerProvider(
        ILogSink[] sinks,
        ILogEnricher[] enrichers,
        LogLevelFilter filter)
    {
        _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
        _enrichers = enrichers ?? throw new ArgumentNullException(nameof(enrichers));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new StructuredLogger(categoryName, _sinks, _enrichers, _filter);
    }

    public void Dispose()
    {
#pragma warning disable CA2012 // Overuse of synchronous methods
        DisposeAllAsync().GetAwaiter().GetResult();
#pragma warning restore CA2012 // Overuse of synchronous methods
    }

    public ValueTask DisposeAsync()
    {
        return DisposeAllAsync();
    }

    private async ValueTask DisposeAllAsync()
    {
        if (_disposed) return;

        foreach (var sink in _sinks)
        {
            await sink.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
