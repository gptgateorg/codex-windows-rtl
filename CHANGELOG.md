# Changelog

All notable changes are documented here.

## [Unreleased]

### Added

- One-time persistent RTL mode that starts a lightweight monitor for the current Windows user
- Automatic reinjection for new or reloaded ChatGPT renderer documents
- Reversible enable and disable controls in the main window

### Fixed

- Microsoft Store builds now launch through their App User Model ID instead of the protected `WindowsApps` executable path
- Mixed English-prefix Persian text now stays right-to-left; direction is based on Persian or Arabic letters anywhere in the paragraph instead of its first word
- RTL observation uses idle-time batching and also rechecks streamed text updates

## [0.4.0] - 2026-07-12

### Added

- Codex-inspired, accessible interface with GptGate website and Telegram links
- Lightweight framework-dependent release target
- Open-source contribution, security, and automation documentation
- Runtime payload checks and automated Windows builds

### Changed

- Unified product identity as GptGate RTL
- Runtime injection now supports Store and regular desktop installations
- Release builds no longer emit separate PDB files

### Removed

- Legacy ASAR modification, backup, and restore workflow
