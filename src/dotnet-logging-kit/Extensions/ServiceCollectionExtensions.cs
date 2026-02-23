using JG.Logging.Abstractions;
using JG.Logging.Internal;
using JG.Logging.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace JG.Logging.Extensions;

/// <summary>
/// Extension methods for registering the structured logging kit with dependency injection.
/// </summary>
public static class LoggingKitServiceCollectionExtensions
{
    /// <summary>
    /// Adds the structured logging kit to the service collection.
    /// </summary>
    /// <param name="services">The service collection to register services in.</param>
    /// <param name="configure">An optional action to configure the logging kit.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddStructuredLogging(
        this IServiceCollection services,
        Action<StructuredLoggingBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new StructuredLoggingBuilder(services);
        configure?.Invoke(builder);

        services.TryAddSingleton<CorrelationIdProvider>();
        services.TryAddSingleton<ScopeContextProvider>();
        
        var filter = builder.GetFilter();
        
        ILogSink[] sinks = builder.GetSinks().Count > 0 
            ? [..builder.GetSinks()] 
            : [new ConsoleSink(new Formatters.PlainTextFormatter())];
        
        ILogEnricher[] enrichers = builder.GetEnrichers().Count > 0
            ? [..builder.GetEnrichers()]
            : [];

        services.AddLogging(logging => 
            logging.AddProvider(new StructuredLoggerProvider(sinks, enrichers, filter)));

        return services;
    }
}

/// <summary>
/// Builder for configuring the structured logging kit.
/// </summary>
public sealed class StructuredLoggingBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<ILogSink> _sinks = [];
    private readonly List<ILogEnricher> _enrichers = [];
    private readonly LogLevelFilter _filter = new();

    /// <summary>
    /// Initializes a new instance of the StructuredLoggingBuilder class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public StructuredLoggingBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Adds a sink to the logging pipeline.
    /// </summary>
    /// <param name="sink">The sink to add.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sink is null.</exception>
    public StructuredLoggingBuilder AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        _sinks.Add(sink);
        return this;
    }

    /// <summary>
    /// Adds a console sink with the specified formatter.
    /// </summary>
    /// <param name="formatter">The formatter to use for console output.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder AddConsoleSink(ILogFormatter? formatter = null)
    {
        formatter ??= new Formatters.PlainTextFormatter();
        return AddSink(new ConsoleSink(formatter));
    }

    /// <summary>
    /// Adds a file sink with the specified configuration.
    /// </summary>
    /// <param name="directory">The directory to write log files to.</param>
    /// <param name="fileNamePrefix">The prefix for log file names.</param>
    /// <param name="maxFileSizeBytes">Maximum size of a single log file in bytes.</param>
    /// <param name="rollingInterval">Interval for rolling files.</param>
    /// <param name="maxBackupFiles">Maximum number of backup files to keep.</param>
    /// <param name="formatter">The formatter to use for file output.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder AddFileSink(
        string directory,
        string fileNamePrefix = "log",
        long maxFileSizeBytes = 10_485_760,
        RollingInterval rollingInterval = RollingInterval.Day,
        int maxBackupFiles = 10,
        ILogFormatter? formatter = null)
    {
        ArgumentNullException.ThrowIfNull(directory);
        formatter ??= new Formatters.JsonFormatter();
        return AddSink(new FileSink(formatter, directory, fileNamePrefix, maxFileSizeBytes, rollingInterval, maxBackupFiles));
    }

    /// <summary>
    /// Adds an enricher to the logging pipeline.
    /// </summary>
    /// <param name="enricher">The enricher to add.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when enricher is null.</exception>
    public StructuredLoggingBuilder AddEnricher(ILogEnricher enricher)
    {
        ArgumentNullException.ThrowIfNull(enricher);
        _enrichers.Add(enricher);
        return this;
    }

    /// <summary>
    /// Adds standard enrichers (machine name, environment, scope context).
    /// </summary>
    /// <param name="includeVersion">Whether to include version enricher for the specified type.</param>
    /// <param name="versionType">The type to extract version information from.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder AddStandardEnrichers(bool includeVersion = false, Type? versionType = null)
    {
        AddEnricher(new Enrichment.MachineNameEnricher());
        AddEnricher(new Enrichment.EnvironmentEnricher());
        AddEnricher(new Enrichment.ScopeContextEnricher());
        
        if (includeVersion && versionType != null)
            AddEnricher(new Enrichment.VersionEnricher(versionType));

        return this;
    }

    /// <summary>
    /// Sets the default minimum log level.
    /// </summary>
    /// <param name="level">The default log level.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder SetMinimumLevel(LogLevel level)
    {
        _filter.DefaultLevel = level;
        return this;
    }

    /// <summary>
    /// Sets the minimum log level for a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="level">The log level for this category.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder SetCategoryLevel(string category, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(category);
        _filter.SetCategoryLevel(category, level);
        return this;
    }

    /// <summary>
    /// Sets the minimum log level for a specific namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace prefix.</param>
    /// <param name="level">The log level for this namespace.</param>
    /// <returns>This builder for chaining.</returns>
    public StructuredLoggingBuilder SetNamespaceLevel(string namespaceName, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);
        _filter.SetNamespaceLevel(namespaceName, level);
        return this;
    }

    internal List<ILogSink> GetSinks() => _sinks;
    internal List<ILogEnricher> GetEnrichers() => _enrichers;
    internal LogLevelFilter GetFilter() => _filter;
}
