# Getting Started with dotnet-logging-kit

This guide will help you get up and running with structured logging in your .NET application.

## Installation

Install the NuGet package:

```bash
dotnet add package JG.LoggingKit
```

## 5-Minute Quick Start

### Basic Console Logging

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JG.Logging.Extensions;

// Create service collection
var services = new ServiceCollection();

// Register structured logging
services.AddStructuredLogging();

// Build service provider
var sp = services.BuildServiceProvider();

// Get logger
var logger = sp.GetRequiredService<ILogger<Program>>();

// Start logging
logger.LogInformation("Application started");
logger.LogWarning("This is a warning");
logger.LogError("An error occurred");
```

### With File Output

```csharp
services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Information);
    
    // Console output
    options.AddConsoleSink(new PlainTextFormatter());
    
    // File output with daily rotation
    options.AddFileSink(
        directory: "logs",
        fileNamePrefix: "myapp",
        rollingInterval: RollingInterval.Day
    );
});
```

## Common Configurations

### ASP.NET Core Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Information);
    
    // Production-ready setup
    options.AddConsoleSink(new JsonFormatter());
    options.AddFileSink(
        "logs",
        rollingInterval: RollingInterval.Day,
        maxBackupFiles: 30
    );
    
    // Add enrichment
    options.AddStandardEnrichers(includeVersion: true, versionType: typeof(Program));
});

var app = builder.Build();

// Middleware to set correlation ID from HTTP header
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers
        .TryGetValue("X-Correlation-ID", out var value)
        ? value.ToString()
        : Guid.NewGuid().ToString();

    using (CorrelationIdProvider.SetCorrelationId(correlationId))
    {
        context.Response.Headers.Add("X-Correlation-ID", correlationId);
        await next();
    }
});

app.Run();
```

### Production Configuration with Log Levels

```csharp
services.AddStructuredLogging(options =>
{
    // Default level
    options.SetMinimumLevel(LogLevel.Warning);
    
    // More verbose for specific areas
    options.SetNamespaceLevel("MyApp.Services", LogLevel.Information);
    options.SetNamespaceLevel("MyApp.Data", LogLevel.Debug);
    
    // Less verbose for noisy components
    options.SetCategoryLevel("MyApp.Telemetry.MetricsCollector", LogLevel.Error);
    
    // Sinks and enrichers
    options.AddConsoleSink(new JsonFormatter());
    options.AddFileSink("logs", rollingInterval: RollingInterval.Day);
    options.AddStandardEnrichers();
});
```

## Using Correlation IDs

Correlation IDs are essential for distributed tracing across your application:

```csharp
using (CorrelationIdProvider.SetCorrelationId(requestId))
{
    logger.LogInformation("Processing request");
    
    await CallServiceAsync();
    await CallDatabaseAsync();
    
    // Correlation ID is automatically included in all logs within this scope
}
```

## Using Scoped Context

Add request-specific properties to your logs:

```csharp
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("UserId", userId);
    ScopeContextProvider.AddPropertyToCurrentScope("RequestPath", context.Request.Path);
    
    logger.LogInformation("Processing user action");
    // All logs in this scope include UserId and RequestPath
}
```

## Structured Data

Include data in your logs for better analysis:

```csharp
// With correlation ID and scoped properties
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("Operation", "DatabaseQuery");
    ScopeContextProvider.AddPropertyToCurrentScope("Duration", "125ms");
    
    logger.LogInformation("Query executed successfully");
}
```

## Error Logging

Log exceptions with full context:

```csharp
try
{
    await riskyOperation();
}
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed for user {UserId}", userId);
}
```

## Output Examples

### Plain Text Format
```
2024-02-23 14:30:45.123 [INFORMATION] {CorrelationId: req-001} Application started
2024-02-23 14:30:46.456 [WARNING] {CorrelationId: req-001} High memory usage detected
2024-02-23 14:30:47.789 [ERROR] {CorrelationId: req-001} Database connection failed [Properties: Duration=125ms]
```

### JSON Format
```json
{
  "timestamp": "2024-02-23T14:30:45.123Z",
  "level": "Information",
  "message": "Application started",
  "correlationId": "req-001",
  "properties": {}
}
```

## Best Practices

### 1. Use Structured Data
Prefer:
```csharp
logger.LogInformation("User {UserId} created an item {ItemId} in {Duration}ms", 
    userId, itemId, duration);
```

Over:
```csharp
logger.LogInformation($"User {userId} created item {itemId} in {duration}ms");
```

### 2. Set Correlation IDs Early
```csharp
using (CorrelationIdProvider.SetCorrelationId(requestId))
{
    // All downstream operations inherit this correlation ID
}
```

### 3. Use Scoped Context for Request-Specific Data
```csharp
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("Request", context.Request.Path);
    // Process request...
}
```

### 4. Configure Log Levels Appropriately
- **Development**: Debug for most areas
- **Staging**: Information for business logic, Debug for key services
- **Production**: Warning by default, Information for important operations, Debug only for troubleshooting

### 5. Use JSON Output in Production
JSON formatting enables structured analysis and integration with log aggregation tools:
```csharp
options.AddConsoleSink(new JsonFormatter());
```

## Custom Enrichers

Create custom enrichers to add your own data:

```csharp
public class RequestIdEnricher : ILogEnricher
{
    private readonly IHttpContextAccessor _contextAccessor;

    public RequestIdEnricher(IHttpContextAccessor contextAccessor)
        => _contextAccessor = contextAccessor;

    public void Enrich(IDictionary<string, object?> properties)
    {
        if (_contextAccessor.HttpContext?.TraceIdentifier is string traceId)
            properties["TraceId"] = traceId;
    }
}

// Register
services.AddStructuredLogging(options =>
{
    options.AddEnricher(new RequestIdEnricher(httpContextAccessor));
});
```

## File Rotation

Configure how log files are managed:

```csharp
// Daily rotation
options.AddFileSink(
    directory: "logs",
    maxFileSizeBytes: 104_857_600,  // 100 MB
    rollingInterval: RollingInterval.Day,
    maxBackupFiles: 30
);

// Size-based rotation (no time-based rolling)
options.AddFileSink(
    directory: "logs",
    maxFileSizeBytes: 10_485_760,  // 10 MB
    rollingInterval: RollingInterval.None,
    maxBackupFiles: 10
);

// Hourly rotation for high-volume logging
options.AddFileSink(
    directory: "logs",
    rollingInterval: RollingInterval.Hour,
    maxBackupFiles: 168  // Keep 1 week of hourly logs
);
```

## Troubleshooting

### Logs not appearing
1. Check `SetMinimumLevel()` - ensure it's not set too high
2. Verify sinks are registered: `AddConsoleSink()` or `AddFileSink()`
3. Check file permissions if using `FileSink`

### File handle errors
1. Ensure `DisposeAsync()` is called on the service provider
2. Don't access log files while they're being written to
3. Check disk space and file permissions

### High memory usage
1. Reduce log level to decrease log volume
2. Use file rotation with size limits
3. Monitor enricher performance

## Next Steps

- Read the [full API reference](../docs/API.md) for comprehensive documentation
- Check out [example applications](../examples/) for real-world usage
- Explore [performance benchmarks](../tests/dotnet-logging-kit.Benchmarks/) to understand throughput

## Getting Help

- Open an issue on [GitHub](https://github.com/jamesgober/dotnet-logging-kit)
- Check [existing issues](https://github.com/jamesgober/dotnet-logging-kit/issues) for solutions
- Review the [API documentation](../docs/API.md) for detailed information
