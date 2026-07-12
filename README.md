# GptGate RTL

A lightweight, open-source Persian RTL companion for the official ChatGPT desktop app on Windows.

GptGate RTL launches the installed app with a local debugging port and injects a small runtime style helper. It does **not** modify Microsoft Store files. Persian and Arabic prose becomes right-to-left while code, terminals, and editable fields stay left-to-right.

- Website: [gptgate.ai](https://gptgate.ai/)
- Telegram: [@GptGateAI](https://t.me/GptGateAI)
- Source: [github.com/gptgate/gptgate-rtl](https://github.com/gptgate/gptgate-rtl)
- Releases: [Download for Windows](https://github.com/gptgate/gptgate-rtl/releases)
- License: [MIT](LICENSE)

## Features

- Live Persian and Arabic RTL support
- Code blocks remain LTR
- Microsoft Store and regular desktop installation detection
- Separate profile or same-profile restart modes
- No installer-file patching
- Accessible WPF UI with Persian labels
- Arad open-source Persian typeface, licensed under SIL OFL 1.1
- Framework-dependent lightweight and self-contained releases

## Requirements

- Windows 10 version 1809 or newer
- The official ChatGPT desktop app
- For the lightweight build: [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```powershell
dotnet build .\CodexRtlManager\CodexRtlManager.csproj -c Release
dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release
```

Optional runtime integration check (opens and then closes a separate ChatGPT profile):

```powershell
dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release -- --runtime "C:\path\to\ChatGPT.exe"
```

Lightweight publish:

```powershell
dotnet publish .\CodexRtlManager\CodexRtlManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\artifacts\light
```

Self-contained publish:

```powershell
dotnet publish .\CodexRtlManager\CodexRtlManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\artifacts\standalone
```

## Architecture

- `CodexLocator` detects supported ChatGPT desktop installations.
- `RuntimeRtlService` starts ChatGPT and communicates only with its loopback DevTools endpoint.
- `RuntimeRtlPayload` contains the small DOM observer that applies RTL to prose.
- `CodexManagerService` coordinates discovery, launch, and safe restart operations.
- `MainWindow` contains the WPF presentation layer.

See [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. Security-sensitive reports should follow [SECURITY.md](SECURITY.md).

## Third-party software

The bundled [Arad typeface](https://github.com/MDarvishi5124/Arad) is copyright the Arad Project Authors and distributed under the SIL Open Font License 1.1. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and the bundled `Arad-OFL.txt`.

## Disclaimer

GptGate RTL is an independent community project. It is not affiliated with or endorsed by OpenAI. ChatGPT, Codex, and OpenAI are trademarks of their respective owner.
