<div align="center">
    <img width="120px" height="auto" src="https://raw.githubusercontent.com/jamesgober/jamesgober/main/media/icons/hexagon-3.svg" alt="Triple Hexagon">
    <h1>
        <strong>dotnet-logging-kit</strong>
        <sup><br><sub>STRUCTURED LOGGING</sub></sup>
    </h1>
    <div>
        <a href="https://www.nuget.org/packages/dotnet-logging-kit"><img alt="NuGet" src="https://img.shields.io/nuget/v/dotnet-logging-kit"></a>
        <span>&nbsp;</span>
        <a href="https://www.nuget.org/packages/dotnet-logging-kit"><img alt="NuGet Downloads" src="https://img.shields.io/nuget/dt/dotnet-logging-kit?color=%230099ff"></a>
        <span>&nbsp;</span>
        <a href="./LICENSE" title="License"><img alt="License" src="https://img.shields.io/badge/license-Apache--2.0-blue.svg"></a>
        <span>&nbsp;</span>
        <a href="https://github.com/jamesgober/dotnet-logging-kit/actions"><img alt="GitHub CI" src="https://github.com/jamesgober/dotnet-logging-kit/actions/workflows/ci.yml/badge.svg"></a>
    </div>
</div>
<br>
<p>
    High-performance structured logging for .NET with automatic request correlation, multiple output formatters, and file rotation. Built on <code>Microsoft.Extensions.Logging</code> with zero-allocation hot paths and minimal overhead in production.
</p>

## Features

- **Correlation IDs** — Automatic request ID propagation across async boundaries via middleware
- **Structured Output** — JSON and console formatters with customizable field layouts
- **File Rotation** — Size-based and time-based log file rotation with configurable retention
- **Log Enrichment** — Attach machine name, environment, version, and custom properties to every entry
- **Scoped Context** — Push/pop contextual properties within using blocks
- **Filtering** — Per-category and per-namespace log level filtering
- **Minimal Overhead** — String interpolation deferred until log level check passes; hot path is allocation-free
- **Single Registration** — `services.AddStructuredLogging()`

<br>

## Installation

```bash
dotnet add package dotnet-logging-kit
```

<br>

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

<br>

## Documentation

- **[API Reference](./docs/API.md)** — Full API documentation and examples

<br>

## Contributing

Contributions welcome. Please:
1. Ensure all tests pass before submitting
2. Follow existing code style and patterns
3. Update documentation as needed

<br>

## Testing

```bash
dotnet test
```

<br>
<hr>
<br>

<div id="license">
    <h2>⚖️ License</h2>
    <p>Licensed under the <b>Apache License</b>, version 2.0 (the <b>"License"</b>); you may not use this software, including, but not limited to the source code, media files, ideas, techniques, or any other associated property or concept belonging to, associated with, or otherwise packaged with this software except in compliance with the <b>License</b>.</p>
    <p>You may obtain a copy of the <b>License</b> at: <a href="http://www.apache.org/licenses/LICENSE-2.0" title="Apache-2.0 License" target="_blank">http://www.apache.org/licenses/LICENSE-2.0</a>.</p>
    <p>Unless required by applicable law or agreed to in writing, software distributed under the <b>License</b> is distributed on an "<b>AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND</b>, either express or implied.</p>
    <p>See the <a href="./LICENSE" title="Software License file">LICENSE</a> file included with this project for the specific language governing permissions and limitations under the <b>License</b>.</p>
    <br>
</div>

<div align="center">
    <h2></h2>
    <sup>COPYRIGHT <small>&copy;</small> 2025 <strong>JAMES GOBER.</strong></sup>
</div>
