using JG.Logging.Abstractions;

namespace JG.Logging.Sinks;

/// <summary>
/// Console sink that writes formatted log entries to the console output stream.
/// </summary>
public sealed class ConsoleSink : ILogSink
{
    private readonly ILogFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of the ConsoleSink class.
    /// </summary>
    /// <param name="formatter">The formatter to use for formatting log entries.</param>
    /// <exception cref="ArgumentNullException">Thrown when formatter is null.</exception>
    public ConsoleSink(ILogFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Writes a formatted log entry to the console.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token for the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var formatted = _formatter.Format(entry);
        Console.WriteLine(formatted);

        return default;
    }

    /// <summary>
    /// Releases resources associated with this sink.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        return default;
    }
}
