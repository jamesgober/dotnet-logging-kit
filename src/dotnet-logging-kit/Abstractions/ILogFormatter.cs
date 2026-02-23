namespace JG.Logging.Abstractions;

/// <summary>
/// Represents a formatter that converts log entries to string output.
/// </summary>
public interface ILogFormatter
{
    /// <summary>
    /// Formats a log entry into a string representation.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <returns>The formatted log output.</returns>
    string Format(LogEntry entry);
}
