namespace JG.Logging.Abstractions;

/// <summary>
/// Represents a scoped context for grouping related log entries with shared properties.
/// </summary>
public interface ILogScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Adds a property to the current scope.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void AddProperty(string key, object? value);

    /// <summary>
    /// Gets all properties in the current scope.
    /// </summary>
    /// <returns>A dictionary of scope properties.</returns>
    IDictionary<string, object?> GetProperties();
}
