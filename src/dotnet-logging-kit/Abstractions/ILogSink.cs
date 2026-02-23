namespace JG.Logging.Abstractions;

/// <summary>
/// Represents a destination for structured log output.
/// </summary>
public interface ILogSink : IAsyncDisposable
{
    /// <summary>
    /// Writes a structured log entry asynchronously.
    /// </summary>
    /// <param name="entry">The log entry to write.</param>
    /// <param name="cancellationToken">Cancellation token for the write operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}
