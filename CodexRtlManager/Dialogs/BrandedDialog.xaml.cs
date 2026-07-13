using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexRtlManager.Dialogs;

public partial class BrandedDialog : Window
{
    private BrandedDialog(
        string title,
        string message,
        string primaryLabel,
        string? secondaryLabel,
        Brush toneBrush)
    {
        InitializeComponent();
        Title = title;
        DialogTitle.Text = title;
        DialogMessage.Text = message;
        PrimaryAction.Content = primaryLabel;
        SecondaryAction.Content = secondaryLabel;
        SecondaryAction.Visibility = secondaryLabel is null ? Visibility.Collapsed : Visibility.Visible;
        ToneBar.Background = toneBrush;

        Loaded += (_, _) =>
        {
            PrimaryAction.Focus();
            if (!SystemParameters.ClientAreaAnimation)
            {
                return;
            }

            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        };
    }

    public static bool ConfirmRestart(Window owner)
    {
        var dialog = Create(
            owner,
            "ChatGPT دوباره اجرا می‌شود",
            "برای فعال‌شدن RTL، همه پنجره‌های باز ChatGPT بسته می‌شوند. برنامه با همان پروفایل دوباره اجرا می‌شود و فایل‌های نصب‌شده تغییر نمی‌کنند.",
            "بستن و اجرای دوباره",
            "انصراف",
            "GoldBrush");

        return RestartConfirmationDecision.ShouldProceed(dialog.ShowDialog());
    }

    public static void ShowInfo(Window? owner, string title, string message)
    {
        Create(owner, title, message, "متوجه شدم", null, "AccentBrush").ShowDialog();
    }

    public static void ShowError(Window? owner, string message)
    {
        Create(owner, "مشکلی پیش آمد", message, "بستن", null, "GoldBrush").ShowDialog();
    }

    private static BrandedDialog Create(
        Window? owner,
        string title,
        string message,
        string primaryLabel,
        string? secondaryLabel,
        string toneResource)
    {
        var toneBrush = Application.Current.FindResource(toneResource) as Brush
            ?? Brushes.DeepSkyBlue;
        var dialog = new BrandedDialog(title, message, primaryLabel, secondaryLabel, toneBrush);

        if (owner?.IsVisible == true)
        {
            dialog.Owner = owner;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        return dialog;
    }

    private void PrimaryAction_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void SecondaryAction_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
