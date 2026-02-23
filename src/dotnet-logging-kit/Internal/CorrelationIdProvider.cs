namespace JG.Logging.Internal;

/// <summary>
/// Thread-safe correlation ID provider for request tracing across the application.
/// </summary>
public sealed class CorrelationIdProvider
{
    private static readonly AsyncLocal<string?> CorrelationIdStorage = new();

    /// <summary>
    /// Gets the current correlation ID for the ambient context.
    /// </summary>
    /// <returns>The correlation ID, or null if not set.</returns>
    public static string? GetCorrelationId() => CorrelationIdStorage.Value;

    /// <summary>
    /// Sets the correlation ID for the ambient context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set, or null to clear.</param>
    /// <returns>A disposable that restores the previous correlation ID when disposed.</returns>
    public static IDisposable SetCorrelationId(string? correlationId)
    {
        var previousValue = CorrelationIdStorage.Value;
        CorrelationIdStorage.Value = correlationId;
        return new CorrelationIdScope(previousValue);
    }

    private sealed class CorrelationIdScope : IDisposable
    {
        private readonly string? _previousValue;
        private bool _disposed;

        public CorrelationIdScope(string? previousValue)
        {
            _previousValue = previousValue;
        }

        public void Dispose()
        {
            if (_disposed) return;
            CorrelationIdStorage.Value = _previousValue;
            _disposed = true;
        }
    }
}
