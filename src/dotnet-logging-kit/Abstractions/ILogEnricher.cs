namespace JG.Logging.Abstractions;

/// <summary>
/// Represents a component that enriches log entries with additional contextual data.
/// </summary>
public interface ILogEnricher
{
    /// <summary>
    /// Enriches a log entry with additional properties or state.
    /// </summary>
    /// <param name="properties">A dictionary where enriched properties should be added.</param>
    void Enrich(IDictionary<string, object?> properties);
}
