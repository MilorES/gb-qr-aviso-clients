using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using GbQrAvisosCliente.Linux.Models;
using GbQrAvisosCliente.Linux.Services;

namespace GbQrAvisosCliente.Linux;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private AppSettings? _settings;
    private MqttAlertListener? _listener;
    private string _statusText = "Preparando…";
    private IBrush _statusBrush = new SolidColorBrush(Color.Parse("#BF701D"));

    public ObservableCollection<AlertItem> Alerts { get; } = [];
    public string StatusText { get => _statusText; private set { _statusText = value; OnPropertyChanged(); } }
    public IBrush StatusBrush { get => _statusBrush; private set { _statusBrush = value; OnPropertyChanged(); } }
    public string AlertCountText => Alerts.Count == 1 ? "1 aviso" : $"{Alerts.Count} avisos";

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        try
        {
            _settings = SettingsStore.LoadOrCreate();
            foreach (var item in AlertHistoryStore.Load(_settings.MaximumAlerts)) Alerts.Add(item);
            OnPropertyChanged(nameof(AlertCountText));
            _listener = new MqttAlertListener(_settings, OnIncomingAlert, SetConnectionStatus);
            _listener.Start();
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Revisa la configuración", false);
            StatusText += $" · {ex.Message}";
        }
    }

    private void SetConnectionStatus(string text, bool connected) => Dispatcher.UIThread.Post(() =>
    {
        StatusText = text;
        StatusBrush = new SolidColorBrush(Color.Parse(connected ? "#21805B" : "#BF701D"));
    });

    private void OnIncomingAlert(AlertItem item) => Dispatcher.UIThread.Post(() =>
    {
        if (Alerts.Count > 0) Alerts[0].IsNewest = false;
        item.IsNewest = true;
        Alerts.Insert(0, item);
        while (_settings is not null && Alerts.Count > _settings.MaximumAlerts)
            Alerts.RemoveAt(Alerts.Count - 1);

        SaveHistory();
        OnPropertyChanged(nameof(AlertCountText));
        WindowState = WindowState.Maximized;
        Show();
        Topmost = true;
        Activate();
    });

    private void Window_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e) => Topmost = false;

    private void AlertRow_Tapped(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: AlertItem { IsNewest: true } item }) return;
        item.IsNewest = false;
        SaveHistory();
        Topmost = false;
        WindowState = WindowState.Minimized;
        e.Handled = true;
    }

    private async void Window_Closed(object? sender, EventArgs e)
    {
        SaveHistory();
        if (_listener is not null) await _listener.DisposeAsync();
    }

    private void SaveHistory()
    {
        if (_settings is not null) AlertHistoryStore.Save(Alerts, _settings.MaximumAlerts);
    }

    private async void OpenSettings_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (IsRunningInWsl())
            {
                await ShowSettingsLocationNotice();
                return;
            }

            var startInfo = new ProcessStartInfo("xdg-open")
            {
                UseShellExecute = false,
                WorkingDirectory = SettingsStore.SettingsDirectory
            };
            startInfo.ArgumentList.Add(SettingsStore.SettingsDirectory);
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            SetConnectionStatus($"No se pudo abrir la configuración: {ex.Message}", false);
        }
    }

    private async Task ShowSettingsLocationNotice()
    {
        var closeButton = new Button
        {
            Content = "Cerrar",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            MinWidth = 100
        };
        var dialog = new Window
        {
            Title = "Configuración de GB Avisos",
            Width = 520,
            Height = 210,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 18,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"En WSL, abre esta carpeta manualmente para editar settings.json o consultar el historial:\n\n{SettingsStore.SettingsDirectory}",
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 16
                    },
                    closeButton
                }
            }
        };
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private static bool IsRunningInWsl()
    {
        var distro = Environment.GetEnvironmentVariable("WSL_DISTRO_NAME");
        if (!string.IsNullOrWhiteSpace(distro)) return true;

        const string osReleasePath = "/proc/sys/kernel/osrelease";
        return File.Exists(osReleasePath) &&
               File.ReadAllText(osReleasePath).Contains("microsoft", StringComparison.OrdinalIgnoreCase);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
