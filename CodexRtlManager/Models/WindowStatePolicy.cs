using System.Windows;

namespace CodexRtlManager.Models;

public static class WindowStatePolicy
{
    public static WindowState Toggle(WindowState current) => current == WindowState.Maximized
        ? WindowState.Normal
        : WindowState.Maximized;
}
