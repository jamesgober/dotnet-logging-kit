# dotnet-logging-kit

[![NuGet](https://img.shields.io/nuget/v/JG.LoggingKit?logo=nuget)](https://www.nuget.org/packages/JG.LoggingKit)
[![Downloads](https://img.shields.io/nuget/dt/JG.LoggingKit?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.LoggingKit)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-logging-kit/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-logging-kit/actions)

A high-performance structured logging library for .NET built on `Microsoft.Extensions.Logging`. Provides correlation ID tracking for distributed tracing, file rotation, multiple output formatters, log enrichment, scoped context, and hierarchical filtering—all with minimal allocations and full async support.

## ✨ Features

- **Correlation IDs** — Automatic request ID propagation across async boundaries
- **Structured Output** — JSON and plain text formatters with exception hierarchies
- **File Rotation** — Size-based and time-based rotation with backup retention
- **Log Enrichment** — Built-in enrichers for system info, environment, version, and properties
- **Scoped Context** — Request-scoped properties with automatic inheritance
- **Advanced Filtering** — Per-category and namespace-based log level control
- **Performance** — Zero-allocation hot paths with `ValueTask<T>` optimization
- **Reliability** — Thread-safe, async-native, comprehensive error handling
- **Production-Ready** — Thoroughly tested, fully documented, battle-hardened

## 📦 Installation

```bash
dotnet add package JG.LoggingKit
```

## 🚀 Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JG.Logging.Extensions;

var services = new ServiceCollection();
services.AddStructuredLogging();

var sp = services.BuildServiceProvider();
var logger = sp.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Application started");
```

## 📚 Documentation

- **[Getting Started](./docs/GETTING-STARTED.md)** — 5-minute quick start with examples
- **[API Reference](./docs/API.md)** — Complete API documentation
- **[Examples](./examples/BasicExample.cs)** — Working code examples
- **[Changelog](./CHANGELOG.md)** — Version history

## ⚙️ Configuration Examples

### Console & File Output
```csharp
services.AddStructuredLogging(options =>
{
    options.SetMinimumLevel(LogLevel.Information);
    options.AddConsoleSink(new JsonFormatter());
    options.AddFileSink("logs", rollingInterval: RollingInterval.Day);
    options.AddStandardEnrichers();
});
```

### Distributed Tracing
```csharp
using (CorrelationIdProvider.SetCorrelationId("request-123"))
{
    logger.LogInformation("Processing request");
    // Correlation ID automatically available in async calls
}
```

### Request-Scoped Properties
```csharp
using (logger.BeginScope(null))
{
    ScopeContextProvider.AddPropertyToCurrentScope("UserId", userId);
    logger.LogInformation("User action"); // Includes UserId
}
```

## 🏗️ Architecture

### Abstractions
- `ILogFormatter` — Format logs to strings
- `ILogSink` — Output destinations
- `ILogEnricher` — Add contextual properties

### Built-In Implementations
- **Formatters**: `PlainTextFormatter`, `JsonFormatter`
- **Sinks**: `ConsoleSink`, `FileSink`
- **Enrichers**: `MachineNameEnricher`, `EnvironmentEnricher`, `VersionEnricher`, `ScopeContextEnricher`

## 📊 Performance

- **Hot Path**: Sub-microsecond log level checks
- **Memory**: Zero allocations for filtered logs
- **Concurrency**: Lock-free correlation ID propagation
- **Throughput**: Validated via benchmarks

## 🧪 Testing

All features are thoroughly tested:
```bash
dotnet test -c Release
```

**36 tests** covering:
- File rotation and backups
- All enrichers and formatters
- Concurrent access patterns
- Edge cases and error scenarios
- Resource disposal

## 🤝 Contributing

Contributions welcome! Please:
- Compile with `TreatWarningsAsErrors=true`
- Include XML documentation
- Add tests for new features
- Follow [PROMPT.md](./dev/PROMPT.md) standards

## 📄 License

Licensed under Apache License 2.0. See [LICENSE](./LICENSE).

---

**[Get started →](./docs/GETTING-STARTED.md)** | **[API docs →](./docs/API.md)**
