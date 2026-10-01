using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GbQrAvisosCliente.Windows.Models;

public sealed class AlertItem : INotifyPropertyChanged
{
    public DateTime ReceivedAt { get; init; }
    public string DisplayText { get; init; } = "";
    private bool _isNewest;
    public bool IsNewest
    {
        get => _isNewest;
        set
        {
            if (_isNewest == value) return;
            _isNewest = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MessageWeight));
            OnPropertyChanged(nameof(RowBackground));
        }
    }

    public FontWeight MessageWeight => IsNewest ? FontWeights.Bold : FontWeights.Normal;
    public Brush RowBackground => IsNewest ? new SolidColorBrush(Color.FromRgb(232, 242, 237)) : Brushes.White;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
