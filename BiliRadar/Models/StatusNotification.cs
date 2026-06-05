using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace BiliRadar.Models;

public sealed class StatusNotification
{
    public StatusNotification(string message, InfoBarSeverity severity)
    {
        Message = message;
        Severity = severity;
    }

    public string Message { get; }

    public InfoBarSeverity Severity { get; }

    internal DispatcherQueueTimer? AutoDismissTimer { get; set; }
}
