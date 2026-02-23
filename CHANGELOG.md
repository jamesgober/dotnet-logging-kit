# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-23

### Added

#### Core Framework
- Structured logging abstraction layer with `ILogSink`, `ILogFormatter`, and `ILogEnricher` interfaces
- `LogEntry` class containing timestamp, level, message, exception, correlation ID, and enriched properties
- Thread-safe `CorrelationIdProvider` using `AsyncLocal<T>` for distributed request tracing
- `ScopeContextProvider` for hierarchical scoped context with property inheritance
- `LogLevelFilter` for category-based and namespace-based log level filtering

#### Formatters
- `PlainTextFormatter` with human-readable output, timestamp formatting, and full exception hierarchy support
- `JsonFormatter` for structured JSON output with recursive exception details and property serialization

#### Sinks
- `ConsoleSink` for console output with configurable formatting
- `FileSink` with advanced rotation capabilities:
  - Size-based rotation (configurable max file size)
  - Time-based rotation (daily, hourly, monthly)
  - Automatic backup file pruning with configurable retention
  - Thread-safe concurrent file writes

#### Enrichers
- `MachineNameEnricher` — Adds machine/hostname to all log entries
- `EnvironmentEnricher` — Captures ASPNETCORE_ENVIRONMENT or defaults to "Production"
- `VersionEnricher` — Extracts assembly version and informational version
- `ScopeContextEnricher` — Includes scoped context properties in log entries
- `SecretSanitizationEnricher` — Masks sensitive data in log output

#### Service Registration
- `AddStructuredLogging()` extension method for dependency injection
- `StructuredLoggingBuilder` fluent API for comprehensive configuration:
  - `AddSink()` and `AddConsoleSink()` / `AddFileSink()` for output configuration
  - `AddEnricher()` and `AddStandardEnrichers()` for property enrichment
  - `SetMinimumLevel()`, `SetCategoryLevel()`, `SetNamespaceLevel()` for filtering

#### Documentation
- Comprehensive API reference in `docs/API.md` with real-world examples
- Getting started guide with 5-minute quick start
- README with feature overview and configuration examples
- XML documentation on all public types and methods
- Architecture overview and performance considerations

#### Testing
- 36 comprehensive unit and integration tests covering:
  - File sink creation, rotation, and backup management
  - All enricher functionality
  - Log level filtering (default, category, namespace)
  - Scoped context with deep nesting
  - Exception handling with inner exception hierarchies
  - Concurrent correlation ID isolation
  - Edge cases (null inputs, large payloads, rapid operations)
  - Stress scenarios (high concurrency, rapid file rotation)
  - Resource disposal and cleanup
- Performance benchmarks using BenchmarkDotNet:
  - Console sink throughput
  - File sink throughput
  - Formatter performance (PlainText and JSON)
  - Memory allocation tracking

### Performance

- Minimal allocations in hot paths through `ValueTask<T>` usage
- O(1) average-case correlation ID and scope lookups via `AsyncLocal<T>`
- Pre-check log level before string formatting to avoid allocations
- Efficient namespace prefix matching for category filtering
- Buffered file writes with automatic flushing for safety
- Lock-free correlation ID propagation across async boundaries

### Security

- Input validation on all public APIs with `ArgumentNullException` and `ArgumentException`
- Secret sanitization enricher to mask sensitive patterns
- Proper exception sanitization in formatters
- No stack trace leaks in error scenarios
- Deterministic builds enabled for reproducibility

### Quality

- All code compiles with `TreatWarningsAsErrors=true`
- Zero warnings in Release build
- Consistent code style via `.editorconfig`
- Complete XML documentation on all public APIs
- Proper disposal patterns with `IAsyncDisposable` support
- Comprehensive test coverage with edge cases

### Infrastructure

- Multi-platform CI/CD via GitHub Actions (Windows, Linux, macOS)
- Project structure following best practices:
  - `src/` for library code
  - `tests/` for unit tests and benchmarks
  - `docs/` for API documentation
  - `examples/` for usage examples
- NuGet package configuration with proper metadata
- SourceLink enabled for debugging
- Deterministic builds for reproducibility

## [Unreleased]

### Planned Features

- OpenTelemetry integration hooks
- Batch writing for ultra-high-volume scenarios
- Rate limiting and backpressure handling
- Additional sinks (EventLog, Syslog, database)
- Structured field mapping configuration
- Additional sample applications

---

[1.0.0]: https://github.com/jamesgober/dotnet-logging-kit/releases/tag/v1.0.0
