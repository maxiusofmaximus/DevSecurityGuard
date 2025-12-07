using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DevSecurityGuard.Launcher
{
    public partial class MainWindow : Window
    {
        private Process? _apiProcess;
        private readonly string _configPath;
        private LauncherConfig _config;

        public MainWindow()
        {
            InitializeComponent();
            
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DevSecurityGuard",
                "launcher-config.json");

            LoadConfig();
            StartApiServer();
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonSerializer.Deserialize<LauncherConfig>(json) ?? new LauncherConfig();
                    
                    // Auto-launch if preference is saved
                    if (_config.RememberChoice && !string.IsNullOrEmpty(_config.PreferredInterface))
                    {
                        StatusText.Text = $"Abriendo {_config.PreferredInterface}...";
                        System.Threading.Tasks.Task.Delay(1500).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                if (_config.PreferredInterface == "Web")
                                    LaunchWeb();
                                else
                                    LaunchDesktop();
                            });
                        });
                    }
                }
                else
                {
                    _config = new LauncherConfig();
                }
            }
            catch
            {
                _config = new LauncherConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void StartApiServer()
        {
            try
            {
                // 1. Try Production Path (API folder next to Launcher)
                var apiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "API");
                
                // 2. Fallback to Dev Path (up 4 levels)
                if (!Directory.Exists(apiPath))
                {
                    apiPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "DevSecurityGuard.API"));
                }
                
                if (Directory.Exists(apiPath))
                {
                    _apiProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "DevSecurityGuard.API.dll --urls=http://localhost:5000",
                            WorkingDirectory = apiPath,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    
                    // If in dev mode (source code), we might need "dotnet run" depending on context, 
                    // but for compiled DLLs 'dotnet DevSecurityGuard.API.dll' is safer.
                    // Let's adjust arguments based on if we found a DLL or a csproj project folder.
                    var dllPath = Path.Combine(apiPath, "DevSecurityGuard.API.dll");
                    if (File.Exists(dllPath))
                    {
                         // Production / Published
                         _apiProcess.StartInfo.Arguments = $"\"{dllPath}\" --urls=http://localhost:5000";
                    }
                    else
                    {
                        // Development / Source (dotnet run)
                        _apiProcess.StartInfo.Arguments = "run --urls=http://localhost:5000";
                    }

                    _apiProcess.Start();
                    
                    // Wait a bit for API to start
                    System.Threading.Tasks.Task.Delay(3000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Text = "Servidor API iniciado ✓";
                        });
                    });
                }
                else
                {
                    StatusText.Text = "⚠ API no encontrada - continuando...";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"⚠ Error al iniciar API: {ex.Message}";
            }
        }

        private void WebButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 0.9;
            }
        }

        private void WebButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
        }

        private void DesktopButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 0.9;
            }
        }

        private void DesktopButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
        }

        private void WebButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (RememberChoiceCheckBox.IsChecked == true)
            {
                _config.RememberChoice = true;
                _config.PreferredInterface = "Web";
                SaveConfig();
            }
            
            LaunchWeb();
        }

        private void DesktopButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (RememberChoiceCheckBox.IsChecked == true)
            {
                _config.RememberChoice = true;
                _config.PreferredInterface = "Desktop";
                SaveConfig();
            }
            
            LaunchDesktop();
        }

        private void LaunchWeb()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5000",
                    UseShellExecute = true
                });
                
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir navegador: {ex.Message}\n\nAbre manualmente: http://localhost:5000", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LaunchDesktop()
        {
            try
            {
                // 1. Try Production Path (UI folder next to Launcher)
                var desktopPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UI", "DevSecurityGuard.UI.exe");

                // 2. Fallback to Dev Path
                if (!File.Exists(desktopPath))
                {
                     desktopPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "DevSecurityGuard.UI", "DevSecurityGuard.UI.exe");
                }
                
                if (File.Exists(desktopPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = desktopPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("App Desktop no encontrada. Usa la interfaz Web.", 
                        "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                    LaunchWeb();
                    return;
                }
                
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir Desktop: {ex.Message}\n\nAbriendo interfaz Web...", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                LaunchWeb();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _apiProcess?.Dispose();
        }
    }

    public class LauncherConfig
    {
        public bool RememberChoice { get; set; }
        public string PreferredInterface { get; set; } = "";
    }
}