using JG.Logging.Abstractions;
using System.Text;
using System.Globalization;

namespace JG.Logging.Formatters;

/// <summary>
/// Plain text log formatter that outputs logs in a simple, human-readable format.
/// </summary>
public sealed class PlainTextFormatter : ILogFormatter
{
    /// <summary>
    /// Formats a log entry into a plain text string representation.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <returns>The formatted log output.</returns>
    public string Format(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var sb = new StringBuilder();
        sb.Append(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        sb.Append(" [");
        sb.Append(entry.Level.ToString().ToUpperInvariant());
        sb.Append(']');

        if (!string.IsNullOrEmpty(entry.CorrelationId))
        {
            sb.Append(" {CorrelationId: ");
            sb.Append(entry.CorrelationId);
            sb.Append('}');
        }

        if (entry.Message != null)
        {
            sb.Append(' ');
            sb.Append(entry.Message);
        }

        if (entry.Properties.Count > 0)
        {
            sb.Append(" [Properties:");
            foreach (var prop in entry.Properties)
            {
                sb.Append(' ');
                sb.Append(prop.Key);
                sb.Append('=');
                sb.Append(prop.Value?.ToString() ?? "null");
            }
            sb.Append(']');
        }

        if (entry.Exception != null)
        {
            sb.AppendLine();
            FormatException(sb, entry.Exception);
        }

        return sb.ToString();
    }

    private static void FormatException(StringBuilder sb, Exception exception, int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);

        sb.Append(indent).Append(exception.GetType().FullName).Append(": ");
        sb.AppendLine(exception.Message);

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            sb.Append(indent).AppendLine("StackTrace:");
            sb.Append(indent).AppendLine(exception.StackTrace);
        }

        if (exception.InnerException != null)
        {
            sb.Append(indent).AppendLine("---> Inner Exception:");
            FormatException(sb, exception.InnerException, indentLevel + 1);
        }
    }
}
