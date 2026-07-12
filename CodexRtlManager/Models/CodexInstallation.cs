namespace CodexRtlManager.Models;

public sealed record CodexInstallation(
    string Version,
    string RootDirectory,
    string AsarPath,
    string ExecutablePath,
    bool IsStorePackage,
    string? AppUserModelId = null);

public enum RuntimeState
{
    NotFound,
    Ready,
    Error
}

public sealed record ManagerSnapshot(
    RuntimeState State,
    string Title,
    string Details,
    CodexInstallation? Installation,
    bool CanLaunch);
