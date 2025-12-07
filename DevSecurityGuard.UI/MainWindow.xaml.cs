using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;

namespace DevSecurityGuard.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        SetupTrayIcon();
        LoadInitialData();
    }

    private void LoadInitialData()
    {
        // Add sample activity items
        AddActivityItem("✅ Service started successfully", "Just now", "#4EC9B0");
        AddActivityItem("📦 Analyzed package: react", "2 minutes ago", "#0E639C");
        AddActivityItem("🚫 Blocked typosquatting: reqest → request", "5 minutes ago", "#F14C4C");
        AddActivityItem("📦 Analyzed package: lodash", "10 minutes ago", "#0E639C");
        AddActivityItem("⚠️ Warning: Package published < 24h ago", "15 minutes ago", "#FFA500");
        AddActivityItem("✅ All detectors loaded successfully", "30 minutes ago", "#4EC9B0");

        // Update statistics (these would come from the service in production)
        UpdateStatistics();
    }

    private void AddActivityItem(string message, string timeAgo, string colorHex)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var messageText = new TextBlock
        {
            Text = message,
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(messageText, 0);

        var timeText = new TextBlock
        {
            Text = timeAgo,
            Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888888")),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 0, 0, 0)
        };
        Grid.SetColumn(timeText, 1);

        grid.Children.Add(messageText);
        grid.Children.Add(timeText);

        ActivityListBox.Items.Add(grid);
    }

    private void UpdateStatistics()
    {
        // In production, these would query the service or database
        ThreatsBlockedText.Text = "3";
        PackagesScannedText.Text = "47";
        DetectorsActiveText.Text = "5";
    }

    // Tray Icon
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "DevSecurityGuard Protection Active";
        
        _notifyIcon.DoubleClick += (s, e) => 
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Open Dashboard", null, (s, e) => { Show(); WindowState = WindowState.Normal; Activate(); });
        contextMenu.Items.Add("Settings", null, (s, e) => OpenSettings());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => { _notifyIcon.Visible = false; System.Windows.Application.Current.Shutdown(); });
        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _notifyIcon?.ShowBalloonTip(2000, "DevSecurityGuard", "Running in background", System.Windows.Forms.ToolTipIcon.Info);
        }
        base.OnStateChanged(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon?.Dispose();
        base.OnClosed(e);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void InterventionModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (InterventionModeCombo.SelectedIndex >= 0)
        {
            var selectedMode = InterventionModeCombo.SelectedIndex switch
            {
                0 => "Automatic",
                1 => "Interactive",
                2 => "Alert Only",
                _ => "Interactive"
            };

            // In production, this would update the service configuration
            StatusText.Text = "UPDATING...";
            
            // Simulate configuration update
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "ACTIVE";
                    AddActivityItem($"⚙️ Changed intervention mode to: {selectedMode}", "Just now", "#0E639C");
                });
            });
        }
    }

    private void ForcePnpmCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (ForcePnpmCheckbox != null)
        {
            var isEnabled = ForcePnpmCheckbox.IsChecked == true;
            AddActivityItem(
                $"⚙️ Force pnpm: {(isEnabled ? "ENABLED" : "DISABLED")}", 
                "Just now", 
                "#0E639C");
        }
    }

    private void EnvProtectionCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (EnvProtectionCheckbox != null)
        {
            var isEnabled = EnvProtectionCheckbox.IsChecked == true;
            AddActivityItem(
                $"⚙️ .env protection: {(isEnabled ? "ENABLED" : "DISABLED")}", 
                "Just now", 
                "#0E639C");
        }
    }

    private void CredentialMonitoringCheckbox_Changed(object sender, RoutedEventArgs e)
    {
        if (CredentialMonitoringCheckbox != null)
        {
            var isEnabled = CredentialMonitoringCheckbox.IsChecked == true;
            AddActivityItem(
                $"⚙️ Credential monitoring: {(isEnabled ? "ENABLED" : "DISABLED")}", 
                "Just now", 
                "#0E639C");
        }
    }

    private void RestartServiceButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Are you sure you want to restart the DevSecurityGuard service?\n\nProtection will be temporarily unavailable during the restart.",
            "Restart Service",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // In production, this would use ServiceController to restart the service
                RestartServiceButton.Content = "⏳ Restarting...";
                RestartServiceButton.IsEnabled = false;
                StatusText.Text = "RESTARTING";

                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        RestartServiceButton.Content = "🔄 Restart Service";
                        RestartServiceButton.IsEnabled = true;
                        StatusText.Text = "ACTIVE";
                        AddActivityItem("🔄 Service restarted successfully", "Just now", "#4EC9B0");
                    });
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to restart service:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            
            if (Directory.Exists(logsPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logsPath,
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show(
                    $"Logs directory not found at:\n{logsPath}\n\nLogs will be created when the service runs.",
                    "Logs Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to open logs directory:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}