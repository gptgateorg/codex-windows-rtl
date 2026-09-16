using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodexRtlManager.Services;

internal static class StoreApplicationActivator
{
    private static readonly Guid ApplicationActivationManagerClassId =
        new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    public static Process Activate(string appUserModelId, string arguments)
    {
        var activationType = Type.GetTypeFromCLSID(ApplicationActivationManagerClassId, throwOnError: true)
            ?? throw new InvalidOperationException("سرویس اجرای برنامه‌های Microsoft Store در دسترس نیست.");
        var manager = (IApplicationActivationManager)(Activator.CreateInstance(activationType)
            ?? throw new InvalidOperationException("سرویس اجرای برنامه‌های Microsoft Store ساخته نشد."));
        try
        {
            _ = CoAllowSetForegroundWindow(manager, IntPtr.Zero);
            var result = manager.ActivateApplication(appUserModelId, arguments, ActivateOptions.None, out var processId);
            Marshal.ThrowExceptionForHR(result);
            return Process.GetProcessById(checked((int)processId));
        }
        finally
        {
            Marshal.FinalReleaseComObject(manager);
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoAllowSetForegroundWindow(
        [MarshalAs(UnmanagedType.IUnknown)] object process,
        IntPtr reserved);

    [ComImport]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments,
            ActivateOptions options,
            out uint processId);

        void ActivateForFile();

        void ActivateForProtocol();
    }

    [Flags]
    private enum ActivateOptions
    {
        None = 0
    }
}
