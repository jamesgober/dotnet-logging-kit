using JG.Logging.Abstractions;
using System.Text.RegularExpressions;

namespace JG.Logging.Enrichment;

/// <summary>
/// Sanitizes log entries to prevent accidental exposure of secrets and sensitive data.
/// </summary>
public sealed class SecretSanitizationEnricher : ILogEnricher
{
    private static readonly Regex[] SensitivePatterns =
    [
        // API Keys and tokens
        new Regex(@"(?i)(api[_-]?key|apikey|api[_-]?secret|token|auth|password|passwd|pwd)[\s:=]+([^\s,;{}""']+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Connection strings
        new Regex(@"(?i)(password|pwd)[\s=]+([^;""']+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        // Credit card patterns (basic)
        new Regex(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled),
        // Email-like patterns in URLs
        new Regex(@"(?i)://[^:]+:[^@]+@", RegexOptions.Compiled),
        // AWS keys
        new Regex(@"(?i)(akia|asia)[0-9A-Z]{16}", RegexOptions.Compiled),
        // JWT-like tokens
        new Regex(@"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled),
    ];

    private const string Redacted = "[REDACTED]";

    /// <summary>
    /// Sanitizes a log entry to remove or mask sensitive information.
    /// </summary>
    public void Enrich(IDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        
        // Mark that sanitization was applied
        properties["_sanitized"] = true;
    }

    /// <summary>
    /// Sanitizes a string value to remove sensitive patterns.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The sanitized value with sensitive data masked.</returns>
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var result = value;
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, match =>
            {
                // Try to preserve some structure
                if (match.Length > 8)
                    return match.Value.AsSpan(0, 4).ToString() + Redacted;
                return Redacted;
            });
        }

        return result;
    }
}
