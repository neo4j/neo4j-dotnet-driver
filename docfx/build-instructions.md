# Building the API documentation locally

These steps generate the Neo4j .NET Driver API reference and articles from `docfx/docfx.json`. **The .NET SDK is assumed to be installed**; DocFX is installed separately below.

The DocFX project uses the **CI** solution configuration and **net10.0** when compiling projects for API metadata (see `docfx.json`). Use a **recent DocFX 2.x** (2.78 or newer is a safe baseline) so Roslyn can parse the current C# language version used by the driver.

---

## 1. Install DocFX

```bash
dotnet tool install -g docfx
```

Confirm it is on your PATH, then check the version:

```bash
docfx --version
```

If `docfx` is not found after install, add the .NET global tools directory to your PATH (the tool install output shows where global tools are placed).

To upgrade later:

```bash
dotnet tool update -g docfx
```

---

## 2. Build and view

From the **repository root**, change into the DocFX folder, then build and start a local web server in one step:

```bash
cd docfx
docfx build docfx.json --serve
```

DocFX builds the docs and serves them from an http server. Open the URL DocFX prints (by default **http://localhost:8080**). Press Ctrl+C to stop the server.