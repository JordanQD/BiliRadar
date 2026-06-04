using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BiliRadar.Models;

public sealed class StatusNotification : INotifyPropertyChanged
{
    public StatusNotification(string message, InfoBarSeverity severity)
    {
        Message = message;
        Severity = severity;
    }

    public string Message { get; }

    public InfoBarSeverity Severity { get; }

    public bool IsOpen => !IsRemoving;

    public bool IsRemoving
    {
        get => _isRemoving;
        private set
        {
            if (_isRemoving == value)
            {
                return;
            }

            _isRemoving = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOpen));
        }
    }
    private bool _isRemoving;

    internal DispatcherQueueTimer? AutoDismissTimer { get; set; }

    internal void BeginRemove()
    {
        IsRemoving = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
