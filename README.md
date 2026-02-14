# DnD Damage Calculator

A D&D Damage Calculator built with ASP.NET Minimal APIs, HTMX, and PicoCSS. Compiled with Native AOT for fast startup and low memory usage.

## Tech Stack

- **.NET 10** — Minimal APIs with `CreateSlimBuilder`
- **Native AOT** — Ahead-of-time compilation for native binaries
- **HTMX 2.0** — Dynamic HTML updates without JavaScript frameworks
- **PicoCSS v2** — Minimal CSS framework for clean styling
- **xUnit** — Integration tests with `WebApplicationFactory`

## Getting Started

```bash
# Clone
git clone https://github.com/igleson/DnDDamageCalc.git
cd DnDDamageCalc

# Run (dev with hot reload)
dotnet watch --project src/DnDDamageCalc.Web

# Run tests
dotnet test

# Publish native AOT binary
dotnet publish src/DnDDamageCalc.Web -c Release
```

## Project Structure

```
DnDDamageCalc/
├── DnDDamageCalc.slnx
├── src/DnDDamageCalc.Web/
│   ├── Program.cs                  # Minimal API endpoints
│   ├── Properties/launchSettings.json
│   └── wwwroot/index.html          # PicoCSS + HTMX UI
└── tests/DnDDamageCalc.Tests/
    └── CounterEndpointTests.cs     # Integration tests
```
