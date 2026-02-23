namespace JG.Logging.Abstractions;

/// <summary>
/// Represents a structured log entry containing all contextual information about a log event.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets the correlation ID associated with this log entry for request tracing.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the log level (Information, Warning, Error, etc.).
    /// </summary>
    public Microsoft.Extensions.Logging.LogLevel Level { get; set; }

    /// <summary>
    /// Gets the event ID associated with this log entry.
    /// </summary>
    public Microsoft.Extensions.Logging.EventId EventId { get; set; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets the exception (if any) associated with this log entry.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets the enriched properties for this log entry.
    /// </summary>
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
}
