# Contributing

Thanks for helping improve GptGate RTL.

## Development setup

1. Install the .NET 8 SDK on Windows.
2. Fork the repository and create a focused branch.
3. Run the build and checks before submitting a pull request:

```powershell
dotnet build .\CodexRtlManager\CodexRtlManager.csproj -c Release
dotnet run --project .\CodexRtlManager.Tests\CodexRtlManager.Tests.csproj -c Release
```

## Project rules

- Keep the installed ChatGPT files untouched.
- Keep code, terminals, and editable controls LTR.
- Do not add telemetry, credentials, or network calls without prior discussion.
- Keep pull requests small and explain user-visible behavior changes.
- Add or update a check for behavior changes.
- Never include build output, PDB files, user profiles, or logs in commits.

## Pull requests

Describe the problem, the approach, manual verification, and any compatibility risk. Screenshots are welcome for UI changes. By contributing, you agree that your work is licensed under the MIT License.
