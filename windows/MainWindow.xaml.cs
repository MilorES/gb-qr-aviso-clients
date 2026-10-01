using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using GbQrAvisosCliente.Windows.Models;
using GbQrAvisosCliente.Windows.Services;

namespace GbQrAvisosCliente.Windows;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private MqttAlertListener? _listener;
    private AppSettings? _settings;
    private bool _isConnected;
    private string _statusText = "Preparando…";

    public ObservableCollection<AlertItem> Alerts { get; } = [];

    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public Brush StatusBrush => _isConnected
        ? new SolidColorBrush(Color.FromRgb(33, 128, 91))
        : new SolidColorBrush(Color.FromRgb(191, 112, 29));

    public string AlertCountText => Alerts.Count == 1 ? "1 aviso" : $"{Alerts.Count} avisos";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = SettingsStore.LoadOrCreate();
            _listener = new MqttAlertListener(_settings, OnIncomingAlert, SetConnectionStatus);
            _listener.Start();
            WindowState = WindowState.Maximized;
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Revisa la configuración", false);
            MessageBox.Show(
                $"No se pudo cargar la configuración.\n\n{ex.Message}\n\nArchivo: {SettingsStore.SettingsPath}",
                "GB Avisos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetConnectionStatus(string text, bool connected)
    {
        Dispatcher.InvokeAsync(() =>
        {
            StatusText = text;
            _isConnected = connected;
            OnPropertyChanged(nameof(StatusBrush));
        });
    }

    private void OnIncomingAlert(AlertItem item)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (Alerts.Count > 0)
                Alerts[0].IsNewest = false;

            item.IsNewest = true;
            Alerts.Insert(0, item);

            while (_settings is not null && Alerts.Count > _settings.MaximumAlerts)
                Alerts.RemoveAt(Alerts.Count - 1);

            OnPropertyChanged(nameof(AlertCountText));
            BringAlertToFront();
        });
    }

    private void BringAlertToFront()
    {
        if (!IsLoaded) return;

        WindowState = WindowState.Maximized;

        Show();
        Topmost = false;
        Topmost = true;
        Activate();

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
            NativeMethods.SetForegroundWindow(handle);
    }

    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e) => Topmost = false;

    private void Window_PreviewTouchDown(object sender, TouchEventArgs e) => Topmost = false;

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e) => Topmost = false;

    private void AlertRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AlertItem item } || !item.IsNewest)
            return;

        AcknowledgeAndMinimize(item);
        e.Handled = true;
    }

    private void AlertRow_TouchDown(object sender, TouchEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AlertItem item } || !item.IsNewest)
            return;

        AcknowledgeAndMinimize(item);
        e.Handled = true;
    }

    private void AcknowledgeAndMinimize(AlertItem item)
    {
        item.IsNewest = false;
        Topmost = false;
        WindowState = WindowState.Minimized;
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_listener is not null)
            await _listener.DisposeAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr handle);
    }
}
