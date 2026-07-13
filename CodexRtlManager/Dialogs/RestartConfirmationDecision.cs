namespace CodexRtlManager.Dialogs;

public static class RestartConfirmationDecision
{
    public static bool ShouldProceed(bool? dialogResult) => dialogResult == true;
}
