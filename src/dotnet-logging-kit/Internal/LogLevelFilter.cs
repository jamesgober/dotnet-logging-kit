using Microsoft.Extensions.Logging;

namespace JG.Logging.Internal;

/// <summary>
/// Manages log level filtering rules for different categories and namespaces.
/// </summary>
public sealed class LogLevelFilter
{
    private readonly Dictionary<string, LogLevel> _categoryRules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LogLevel> _namespaceRules = new(StringComparer.OrdinalIgnoreCase);
    private LogLevel _defaultLevel = LogLevel.Information;

    /// <summary>
    /// Gets or sets the default minimum log level.
    /// </summary>
    public LogLevel DefaultLevel
    {
        get => _defaultLevel;
        set => _defaultLevel = value;
    }

    /// <summary>
    /// Sets the minimum log level for a specific category.
    /// </summary>
    /// <param name="category">The category name (e.g., "MyApp.Services.UserService").</param>
    /// <param name="level">The minimum log level for this category.</param>
    public void SetCategoryLevel(string category, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(category);
        _categoryRules[category] = level;
    }

    /// <summary>
    /// Sets the minimum log level for a specific namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace prefix (e.g., "MyApp.Services").</param>
    /// <param name="level">The minimum log level for this namespace.</param>
    public void SetNamespaceLevel(string namespaceName, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);
        _namespaceRules[namespaceName] = level;
    }

    /// <summary>
    /// Determines if a log level is enabled for a given category.
    /// </summary>
    /// <param name="category">The log category.</param>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns>True if the log level is enabled for this category; otherwise false.</returns>
    public bool IsEnabled(string category, LogLevel logLevel)
    {
        ArgumentNullException.ThrowIfNull(category);

        var requiredLevel = GetLevelForCategory(category);
        return logLevel >= requiredLevel;
    }

    private LogLevel GetLevelForCategory(string category)
    {
        if (_categoryRules.TryGetValue(category, out var categoryLevel))
            return categoryLevel;

        foreach (var namespacePrefix in _namespaceRules.Keys.OrderByDescending(k => k.Length))
        {
            if (category.StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase))
                return _namespaceRules[namespacePrefix];
        }

        return _defaultLevel;
    }
}
