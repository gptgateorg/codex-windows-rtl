# AGENTS.md

## Project

GptGate RTL is a Windows WPF app that launches the official ChatGPT desktop app with runtime Persian RTL support. The installed app files must remain untouched.

## Commands

- Build: `dotnet build .\CodexRtlManager\CodexRtlManager.csproj -c Release`
- Check: `dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release`
- Light publish: `dotnet publish .\CodexRtlManager\CodexRtlManager.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o .\artifacts\light`
- Standalone publish: `dotnet publish .\CodexRtlManager\CodexRtlManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\artifacts\standalone`

## Invariants

- Never modify or replace installed ChatGPT or Microsoft Store files.
- DevTools communication must stay on loopback.
- Persian and Arabic prose may be RTL; code and editable fields must remain LTR.
- Do not add telemetry, secrets, or new network destinations.
- Preserve accessible names and keyboard operation in UI changes.
- Add a check for every behavior change and run all commands before handoff.
