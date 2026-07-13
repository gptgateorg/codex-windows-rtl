using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CodexRtlManager.Dialogs;
using CodexRtlManager.Models;
using CodexRtlManager.Services;
using Microsoft.Win32;

namespace CodexRtlManager;

public partial class MainWindow : Window
{
    private const string WebsiteUrl = "https://gptgate.ai/";
    private const string TelegramUrl = "https://t.me/GptGateAI";

    private readonly CodexManagerService _manager = new();
    private readonly UpdateService _updates = new();
    private bool _busy;
    private ManagerSnapshot? _snapshot;

    public MainWindow()
    {
        InitializeComponent();
        FontNameText.Text = _manager.ActiveFontName;
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        SetBusy(true, "در حال بررسی نسخه دسکتاپ ChatGPT…");
        var snapshot = await _manager.GetSnapshotAsync();
        Render(snapshot);
        SetBusy(false);
        ProgressText.Text = snapshot.CanLaunch
            ? "آماده اجرا با تزریق RTL در حافظه؛ هیچ فایل نصبی تغییر نمی‌کند."
            : "پس از نصب ChatGPT Desktop دوباره بررسی کنید.";
    }

    private void Render(ManagerSnapshot snapshot)
    {
        _snapshot = snapshot;
        StatusTitle.Text = snapshot.Title;
        StatusDetails.Text = snapshot.Details;
        StatusDetails.Visibility = snapshot.State == RuntimeState.Ready
            ? Visibility.Collapsed
            : Visibility.Visible;
        CodexVersionText.Text = snapshot.Installation?.Version ?? "—";
        InstallTypeText.Text = snapshot.Installation is null
            ? "شناسایی نشد"
            : snapshot.Installation.IsStorePackage ? "Microsoft Store" : "نصب معمولی";

        StatusDot.Fill = new SolidColorBrush(snapshot.State switch
        {
            RuntimeState.Ready => Color.FromRgb(48, 209, 88),
            RuntimeState.Error => Color.FromRgb(255, 69, 58),
            _ => Color.FromRgb(114, 114, 114)
        });

        LaunchButton.IsEnabled = snapshot.CanLaunch && !_busy;
        RestartButton.IsEnabled = snapshot.CanLaunch && !_busy;
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        await RunLaunchAsync(
            () => _manager.LaunchAsync(useExistingProfile: false, new Progress<string>(SetProgress)),
            "ChatGPT با پروفایل جدا و RTL اجرا شد.");
    }

    private async void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_manager.IsChatGptRunning())
        {
            if (!BrandedDialog.ConfirmRestart(this))
            {
                return;
            }
        }

        await RunLaunchAsync(
            () => _manager.RestartCurrentChatGptWithRtlAsync(new Progress<string>(SetProgress)),
            "ChatGPT با همان پروفایل و RTL اجرا شد.");
    }

    private async Task RunLaunchAsync(Func<Task> operation, string successMessage)
    {
        SetBusy(true, "در حال اجرای ChatGPT و فعال‌کردن RTL…");
        try
        {
            await operation();
            ProgressText.Text = successMessage;
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true, "در حال بررسی بروزرسانی…");
        try
        {
            var current = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 4, 0);
            var result = await _updates.CheckAsync(current);
            BrandedDialog.ShowInfo(this, "بروزرسانی", result.Message);
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SelectFontButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "انتخاب فونت متن فارسی",
            Filter = "Font files (*.ttf;*.otf)|*.ttf;*.otf",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            FontNameText.Text = _manager.ImportFont(dialog.FileName);
            ProgressText.Text = "فونت ذخیره شد و در اجرای بعدی ChatGPT اعمال می‌شود.";
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void ResetFontButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _manager.ResetFont();
            FontNameText.Text = _manager.ActiveFontName;
            ProgressText.Text = "فونت پیش‌فرض Arad در اجرای بعدی ChatGPT اعمال می‌شود.";
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void WebsiteButton_Click(object sender, RoutedEventArgs e) => OpenUrl(WebsiteUrl);

    private void TelegramButton_Click(object sender, RoutedEventArgs e) => OpenUrl(TelegramUrl);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState()
    {
        WindowState = WindowStatePolicy.Toggle(WindowState);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            ShowError(exception);
        }
    }

    private void SetProgress(string message) => ProgressText.Text = message;

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        WorkProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        if (message is not null)
        {
            ProgressText.Text = message;
        }

        LaunchButton.IsEnabled = !busy && _snapshot?.CanLaunch == true;
        RestartButton.IsEnabled = !busy && _snapshot?.CanLaunch == true;
        UpdateButton.IsEnabled = !busy;
        SelectFontButton.IsEnabled = !busy;
        ResetFontButton.IsEnabled = !busy;
    }

    private static void ShowError(Exception exception)
    {
        BrandedDialog.ShowError(Application.Current?.MainWindow, exception.Message);
    }
}
