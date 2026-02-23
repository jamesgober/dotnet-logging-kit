# API Reference

## Overview

The dotnet-logging-kit provides a flexible, high-performance structured logging system built on `Microsoft.Extensions.Logging`. It supports multiple output formatters, custom enrichers, log rotation, and correlation ID tracking.

## Core Concepts

### Log Entries
A `LogEntry` represents a single log event with the following properties:
- `Timestamp` — When the entry was created (UTC)
- `Level` — The log level (Debug, Information, Warning, Error, Critical)
- `Message` — The formatted log message
- `Exception` — Any associated exception
- `CorrelationId` — Request ID for distributed tracing
- `Properties` — Custom enriched properties

### Sinks
A sink is a destination for log entries. Available implementations:
- `ConsoleSink` — Writes to console output
- `FileSink` — Writes to rotating log files

### Formatters
A formatter converts log entries to strings. Built-in implementations:
- `PlainTextFormatter` — Human-readable text format
- `JsonFormatter` — Machine-parseable JSON format

### Enrichers
Enrichers add contextual properties to log entries. Built-in implementations:
- `MachineNameEnricher` — Adds the machine name
- `EnvironmentEnricher` — Adds environment information
- `VersionEnricher` — Adds assembly version information
- `ScopeContextEnricher` — Adds scoped context properties

## Service Registration

### Basic Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using JG.Logging.Extensions;

var services = new ServiceCollection();
services.AddStructuredLogging();
var sp = services.BuildServiceProvider();
```

### Advanced Configuration

```csharp
services.AddStructuredLogging(options =>
{
    // Set minimum log level
    options.SetMinimumLevel(LogLevel.Information);
    
    // Add console sink with JSON output
    options.AddConsoleSink(new JsonFormatter());
    
    // Add file sink with daily rotation
    options.AddFileSink(
        directory: "logs",
        fileNamePrefix: "app",
        rollingInterval: RollingInterval.Day
    );
    
    // Add standard enrichers
    options.AddStandardEnrichers(includeVersion: true, versionType: typeof(Program));
    
    // Add custom enricher
    options.AddEnricher(new MyCustomEnricher());
});
```

## Usage Examples

### Basic Logging

```csharp
var logger = sp.GetRequiredService<ILogger<MyService>>();

logger.LogInformation("Application started");
logger.LogWarning("This is a warning");
logger.LogError("An error occurred: {error}", errorMessage);
```

### Correlation IDs

Correlation IDs are automatically propagated across async calls within a scope:

```csharp
using (JG.Logging.Internal.CorrelationIdProvider.SetCorrelationId("request-id-123"))
{
    // All logs in this scope will include correlation ID
    logger.LogInformation("Processing request");
    await ProcessAsync(); // Correlation ID is available in async calls
}
```

### Scoped Context

Add contextual properties for a specific scope:

```csharp
using (var scope = logger.BeginScope(null))
{
    JG.Logging.Internal.ScopeContextProvider.AddPropertyToCurrentScope("UserId", "user-456");
    JG.Logging.Internal.ScopeContextProvider.AddPropertyToCurrentScope("RequestId", "req-789");
    
    logger.LogInformation("User action"); // Includes UserId and RequestId
}
```

### Custom Sinks

Implement `ILogSink` to create custom log destinations:

```csharp
public class DatabaseSink : ILogSink
{
    private readonly IDatabase _db;

    public DatabaseSink(IDatabase db) => _db = db;

    public async ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        await _db.InsertLogAsync(entry, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}

// Register
services.AddStructuredLogging(options =>
{
    options.AddSink(new DatabaseSink(database));
});
```

### Custom Enrichers

Implement `ILogEnricher` to add custom properties:

```csharp
public class UserContextEnricher : ILogEnricher
{
    private readonly IUserContext _userContext;

    public UserContextEnricher(IUserContext userContext) => _userContext = userContext;

    public void Enrich(IDictionary<string, object?> properties)
    {
        if (_userContext.CurrentUser != null)
        {
            properties["UserId"] = _userContext.CurrentUser.Id;
            properties["UserName"] = _userContext.CurrentUser.Name;
        }
    }
}

// Register
services.AddStructuredLogging(options =>
{
    options.AddEnricher(new UserContextEnricher(userContext));
});
```

### Custom Formatters

Implement `ILogFormatter` for custom output:

```csharp
public class CsvFormatter : ILogFormatter
{
    public string Format(LogEntry entry)
    {
        return $"{entry.Timestamp:O},{entry.Level},{entry.Message}";
    }
}

// Register
services.AddStructuredLogging(options =>
{
    options.AddSink(new FileSink(new CsvFormatter(), "logs"));
});
```

## Log Level Filtering

Control which log levels are enabled per category or namespace:

```csharp
services.AddStructuredLogging(options =>
{
    // Global minimum level
    options.SetMinimumLevel(LogLevel.Information);
    
    // Category-specific level (exact match)
    options.SetCategoryLevel("MyApp.Data.Repository", LogLevel.Debug);
    
    // Namespace-level filtering (prefix match)
    options.SetNamespaceLevel("MyApp.Services", LogLevel.Warning);
});
```

## File Rotation

The `FileSink` supports multiple rotation strategies:

### Size-Based Rotation
```csharp
options.AddFileSink(
    directory: "logs",
    maxFileSizeBytes: 10_485_760, // 10 MB
    rollingInterval: RollingInterval.None
);
```

### Time-Based Rotation
```csharp
// Daily rotation
options.AddFileSink(
    directory: "logs",
    rollingInterval: RollingInterval.Day
);

// Hourly rotation
options.AddFileSink(
    directory: "logs",
    rollingInterval: RollingInterval.Hour
);

// Monthly rotation
options.AddFileSink(
    directory: "logs",
    rollingInterval: RollingInterval.Month
);
```

### Backup File Retention
```csharp
options.AddFileSink(
    directory: "logs",
    maxBackupFiles: 30 // Keep last 30 files
);
```

## Configuration Examples

### ASP.NET Core with Correlation IDs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Information);
    options.AddConsoleSink(new JsonFormatter());
    options.AddFileSink("logs", rollingInterval: RollingInterval.Day);
    options.AddStandardEnrichers(includeVersion: true, versionType: typeof(Program));
});

var app = builder.Build();

// Add middleware to set correlation ID from request
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var value)
        ? value.ToString()
        : Guid.NewGuid().ToString();

    using (JG.Logging.Internal.CorrelationIdProvider.SetCorrelationId(correlationId))
    {
        context.Response.Headers.Add("X-Correlation-ID", correlationId);
        await next();
    }
});

app.Run();
```

### High-Volume Logging with File Rotation

```csharp
services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Warning);
    
    // Multiple sinks for different purposes
    options.AddFileSink(
        "logs/errors",
        fileNamePrefix: "errors",
        rollingInterval: RollingInterval.Day,
        maxBackupFiles: 30
    );
    
    options.AddFileSink(
        "logs/archive",
        fileNamePrefix: "archive",
        maxFileSizeBytes: 52_428_800, // 50 MB
        rollingInterval: RollingInterval.None
    );
    
    options.AddStandardEnrichers();
});
```

## Thread Safety

All components are thread-safe for use in multi-threaded applications:
- `CorrelationIdProvider` uses `AsyncLocal<T>` for isolated context per async flow
- `ScopeContextProvider` uses `AsyncLocal<Stack<T>>` for scope hierarchy
- `FileSink` uses locking for thread-safe file access

## Performance Considerations

- **Hot Path Optimization**: Correlation ID and scope context lookups are O(1) average case
- **Allocation Reduction**: Sinks and formatters use `ValueTask<T>` to avoid heap allocations when possible
- **File I/O**: `FileSink` uses buffered writes with `AutoFlush = true` for safe high-throughput logging
- **Filtering**: Log level checks happen before string formatting to avoid unnecessary allocations

## Disposal

Always dispose of the service provider to ensure sinks are properly closed:

```csharp
using var sp = services.BuildServiceProvider();
var logger = sp.GetRequiredService<ILogger>();

logger.LogInformation("Application running");

// Disposal is automatically called when exiting using block
```

Or explicitly:

```csharp
await sp.DisposeAsync();
```
