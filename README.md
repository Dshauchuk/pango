# Pango

A **Windows** password manager that stores credentials on disk with encryption, supports folder-style catalogs, import and export, a password generator, and an optional background backup **Windows Service**.

---

## Features

- **Entries** — title, target resource, login, password, custom properties, favorites.
- **Catalogs** — hierarchical organization (including placeholder entries that act as folders).
- **Security** — sensitive values are protected in memory (`RamProtectedString`); on-disk data is encoded using the current user context and encryption settings.
- **Import and export** — packaged data with a manifest and selected entries.
- **Auto-lock** — optional idle timeout that returns the user to the sign-in screen.
- **Backups** — a separate service zips user data, encrypts the archive, and rotates old backups according to a retention policy.

## Why Pango

Pango is aimed at people who want **local-first** credential storage rather than a mandatory cloud account.

- **You own the data** — vault files live on disk under your control; there is no vendor lock-in to a hosted backend.
- **Smaller network exposure** — no required sync channel or central API; backups can stay on your own drives or NAS when you configure the backup service.
- **Privacy by default** — usage metadata is not part of someone else’s SaaS business model.
- **Works offline** — full access without depending on CDN uptime or provider availability.
- **Fits strict environments** — useful where cloud password services are not allowed.

Trade-offs: you do not get turnkey cross-device sync or hosted account recovery out of the box—this repository does **not** implement cloud sync by design.

## Stack and architecture

| Layer | Role |
|------|------|
| **Pango.Domain** | Domain entities and rules. |
| **Pango.Application** | Use cases (MediatR, CQRS), DTOs, contracts. |
| **Pango.Infrastructure** | Service implementations (including backup). |
| **Pango.Persistence / Pango.Persistence.File** | File-backed storage (`passwords` directory under the user data profile). |
| **Pango.Desktop.Uwp** | **WinUI 3** client (UWP-style project), MVVM, DI via `Microsoft.Extensions.Hosting`. |
| **Pango.BackupService** | Windows background service (`Microsoft.Extensions.Hosting.WindowsServices`). |

The client uses **Serilog**, with output to the debugger and the Windows Event Log (source name `PangoApp`).

## Requirements

- **OS:** Windows 10 **1809** or later (minimum target platform in the project: `10.0.18362.0`).
- **SDK / runtime:** **.NET 10** (`net10.0-windows10.0.19041.0`) and the Windows SDK required to build the app package.
- **Tooling:** Visual Studio 2022 or newer with the desktop + **WinUI** workload, or the matching .NET SDK and MSBuild for command-line builds.

Client build platforms: **x86**, **x64**, **ARM64**.

## Build and run

```bash
# from the repository root
dotnet restore Pango.sln
dotnet build Pango.sln -c Debug
```

**NuGet:** package versions are centralized in [`Directory.Packages.props`](Directory.Packages.props) (SDK central package management). Shared compile defaults (`LangVersion`, nullable, implicit usings) live in [`Directory.Build.props`](Directory.Build.props). [`NuGet.config`](NuGet.config) pins restores to **nuget.org** with `packageSourceMapping` (add private feeds there if you need them).

To check for outdated packages: `dotnet list Pango.sln package --outdated`.

For packaging and debugging the WinUI app, open `Pango.sln` in Visual Studio and set **Pango.Desktop.Uwp** as the startup project.

Tests:

```bash
dotnet test Pango.sln -c Debug
```

- **Unit tests:** `Pango.Application.Tests`, `Pango.Infrastructure.Tests` (mocked dependencies).
- **Integration tests:** `Pango.Persistence.File.IntegrationTests` — real `ContentEncoder` + `PasswordFileRepository` against a temporary on-disk folder.

### Code coverage report (ReportGenerator)

The repo includes a [local dotnet tool](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool) (`dotnet-tools.json`). From the repository root:

```bash
dotnet tool restore
./scripts/Generate-CoverageReport.ps1
```

This runs all tests with **Coverlet** (cobertura), merges assemblies, and writes HTML to `artifacts/coverage-report/index.html`. Ensure NuGet can restore the ReportGenerator package (use the public NuGet gallery if private feeds return 401).

## Backup service

`Pango.BackupService` is a separate executable installed as a **Windows Service**. Configuration is read from:

`%ProgramData%\Pango\backup_config.json`

The file defines users, data paths, backup encryption password, destination folder, interval, and retention. Output files are named like `*-{userId}_Backup.pngx` (encrypted archive).

## Repository layout

```
src/
  Core/            — Domain, Application
  Infrastructure/  — Infrastructure, Persistence, Persistence.File
  Presentation/    — Pango.Desktop.Uwp
  Services/        — Pango.BackupService
  Shared/          — Pango.Shared
tests/             — unit tests; Pango.Persistence.File.IntegrationTests (file persistence)
scripts/           — Generate-CoverageReport.ps1 (merged HTML coverage)
Directory.Packages.props — central NuGet package versions
Directory.Build.props    — shared MSBuild properties (CPM enabled)
NuGet.config             — package sources / source mapping
```

## License

Released under the [MIT License](LICENSE).
