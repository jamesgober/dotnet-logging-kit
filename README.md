# dotnet-logging-kit

[![NuGet](https://img.shields.io/nuget/v/JG.LoggingKit?logo=nuget)](https://www.nuget.org/packages/JG.LoggingKit)
[![Downloads](https://img.shields.io/nuget/dt/JG.LoggingKit?color=%230099ff&logo=nuget)](https://www.nuget.org/packages/JG.LoggingKit)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](./LICENSE)
[![CI](https://github.com/jamesgober/dotnet-logging-kit/actions/workflows/ci.yml/badge.svg)](https://github.com/jamesgober/dotnet-logging-kit/actions)

---

High-performance structured logging for .NET with automatic request correlation, multiple output formatters, and file rotation. Built on `Microsoft.Extensions.Logging` with zero-allocation hot paths and minimal overhead in production.


## Features

- **Correlation IDs** — Automatic request ID propagation across async boundaries via middleware
- **Structured Output** — JSON and console formatters with customizable field layouts
- **File Rotation** — Size-based and time-based log file rotation with configurable retention
- **Log Enrichment** — Attach machine name, environment, version, and custom properties to every entry
- **Scoped Context** — Push/pop contextual properties within using blocks
- **Filtering** — Per-category and per-namespace log level filtering
- **Minimal Overhead** — String interpolation deferred until log level check passes; hot path is allocation-free
- **Single Registration** — `services.AddStructuredLogging()`

## Installation

```bash
dotnet add package JG.LoggingKit
```

## Quick Start

```csharp
builder.Services.AddStructuredLogging(options =>
{
    options.MinimumLevel = LogLevel.Information;
    options.AddConsoleJson();
    options.AddFile("logs/app.log", rollingInterval: RollingInterval.Day);
    options.Enrich.WithMachineName();
    options.Enrich.WithEnvironment();
});
```

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

Licensed under the Apache License 2.0. See [LICENSE](./LICENSE) for details.

---

**Ready to get started?** Install via NuGet and check out the [API reference](./docs/API.md).
