// Example: Basic Structured Logging with dotnet-logging-kit
// This example demonstrates core logging functionality.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JG.Logging.Extensions;
using JG.Logging.Formatters;
using JG.Logging.Internal;
using JG.Logging.Sinks;

var services = new ServiceCollection();

// Configure structured logging
services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Information);
    
    // Console output with plain text formatting
    options.AddConsoleSink(new PlainTextFormatter());
    
    // File output with JSON formatting and daily rotation
    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    options.AddFileSink(
        directory: logsDir,
        fileNamePrefix: "example",
        rollingInterval: JG.Logging.Sinks.RollingInterval.Day,
        maxBackupFiles: 7
    );
    
    // Add enrichers for context
    options.AddStandardEnrichers(includeVersion: true, versionType: typeof(Program));
});

var sp = services.BuildServiceProvider();
var logger = sp.GetRequiredService<ILogger<Program>>();

// Example 1: Basic logging
logger.LogInformation("Application started");

// Example 2: Logging with correlation ID
using (CorrelationIdProvider.SetCorrelationId(Guid.NewGuid().ToString()))
{
    logger.LogInformation("Processing user request");
    
    // Simulate async operation
    await Task.Delay(100);
    logger.LogInformation("Request completed");
}

// Example 3: Scoped context for request-specific data
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("UserId", "user-123");
    ScopeContextProvider.AddPropertyToCurrentScope("RequestPath", "/api/users");
    
    logger.LogInformation("Handling API request");
}

// Example 4: Error logging with exceptions
try
{
    throw new InvalidOperationException("Example error for demonstration");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred during processing");
}

// Example 5: Structured data with enrichment
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("Operation", "DatabaseQuery");
    ScopeContextProvider.AddPropertyToCurrentScope("Duration", "125ms");
    
    logger.LogInformation("Query executed successfully");
}

logger.LogInformation("Application shutting down");

// Dispose to ensure file sinks are flushed
await sp.DisposeAsync();
