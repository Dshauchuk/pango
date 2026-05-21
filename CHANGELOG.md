# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html) for public API and release tagging where applicable.

## [Unreleased]

### Changed

- Centralized product and assembly versioning in `build/Version.props` (see README).
- MSIX package identity version is generated at build from `AssemblyVersion` (no manual sync in manifest XML); Debug vs Release manifest selection uses `$(Configuration)`.

## [0.1.6] - 2026-04-11

### Added

- Central Package Management (`Directory.Packages.props`) and shared MSBuild defaults (`Directory.Build.props`).
- Unit and integration tests; optional HTML coverage via ReportGenerator (`scripts/Generate-CoverageReport.ps1`).
- Repository `NuGet.config` for deterministic restores with package source mapping.
- Alerting about expiring(ed) passwords.
- A Password generator.
- Backup of the application data.
- New application configurations.
- Minor bug fixes.

### Fixed

- Distinct error codes for password creation vs deletion; improved change-password flow and error reporting.

### Notes

- MSIX identity version is stamped at build from `build/Version.props` (`AssemblyVersion`).
