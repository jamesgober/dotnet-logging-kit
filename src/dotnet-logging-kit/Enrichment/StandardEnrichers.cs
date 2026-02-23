using JG.Logging.Abstractions;
using System.Reflection;

namespace JG.Logging.Enrichment;

/// <summary>
/// Enriches log entries with machine/environment information.
/// </summary>
public sealed class MachineNameEnricher : ILogEnricher
{
    private static readonly string MachineName = Environment.MachineName;

    /// <summary>
    /// Enriches a log entry with the machine name.
    /// </summary>
    public void Enrich(IDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (!properties.ContainsKey("MachineName"))
            properties["MachineName"] = MachineName;
    }
}

/// <summary>
/// Enriches log entries with environment information.
/// </summary>
public sealed class EnvironmentEnricher : ILogEnricher
{
    private static readonly string Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    /// <summary>
    /// Enriches a log entry with the current environment.
    /// </summary>
    public void Enrich(IDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (!properties.ContainsKey("Environment"))
            properties["Environment"] = Environment;
    }
}

/// <summary>
/// Enriches log entries with assembly version information.
/// </summary>
public sealed class VersionEnricher : ILogEnricher
{
    private readonly string _version;
    private readonly string _informationalVersion;

    /// <summary>
    /// Initializes a new instance of VersionEnricher for a specific type.
    /// </summary>
    /// <param name="type">The type from the assembly to extract version from.</param>
    public VersionEnricher(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var assembly = type.Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? version;

        _version = version;
        _informationalVersion = infoVersion;
    }

    /// <summary>
    /// Enriches a log entry with version information.
    /// </summary>
    public void Enrich(IDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        if (!properties.ContainsKey("Version"))
            properties["Version"] = _version;
        if (!properties.ContainsKey("InformationalVersion"))
            properties["InformationalVersion"] = _informationalVersion;
    }
}

/// <summary>
/// Enriches log entries with scope context properties.
/// </summary>
public sealed class ScopeContextEnricher : ILogEnricher
{
    /// <summary>
    /// Enriches a log entry with properties from the current scope context.
    /// </summary>
    public void Enrich(IDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var scopeProps = Internal.ScopeContextProvider.GetScopeProperties();
        foreach (var kvp in scopeProps)
        {
            if (!properties.ContainsKey(kvp.Key))
                properties[kvp.Key] = kvp.Value;
        }
    }
}
