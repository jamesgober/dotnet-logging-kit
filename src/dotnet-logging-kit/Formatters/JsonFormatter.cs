using JG.Logging.Abstractions;
using System.Text.Json;

namespace JG.Logging.Formatters;

/// <summary>
/// JSON log formatter that outputs logs in JSON format for structured parsing.
/// </summary>
public sealed class JsonFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Formats a log entry into a JSON string representation.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <returns>The formatted log output in JSON.</returns>
    public string Format(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var exceptionInfo = entry.Exception != null ? FormatException(entry.Exception) : null;

        var logObject = new
        {
            timestamp = entry.Timestamp.ToString("o"),
            level = entry.Level.ToString(),
            eventId = entry.EventId.Id,
            message = entry.Message,
            correlationId = entry.CorrelationId,
            exception = exceptionInfo,
            properties = entry.Properties
        };

        return JsonSerializer.Serialize(logObject, DefaultOptions);
    }

    private static object? FormatException(Exception exception)
    {
        return new
        {
            type = exception.GetType().FullName,
            message = exception.Message,
            stackTrace = exception.StackTrace,
            innerException = exception.InnerException != null ? FormatException(exception.InnerException) : null
        };
    }
}
