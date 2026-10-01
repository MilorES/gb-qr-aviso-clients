using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace GbQrAvisosCliente.Linux.Models;

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
        }
    }

    public FontWeight MessageWeight => IsNewest ? FontWeight.Bold : FontWeight.Normal;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
