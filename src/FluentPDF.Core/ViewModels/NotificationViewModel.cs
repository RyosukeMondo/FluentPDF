using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Services;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// ViewModel for toast notification display. Manages a stack of notifications
/// that auto-dismiss after their timeout expires.
/// </summary>
public partial class NotificationViewModel : ViewModelBase, INotificationService
{
    public ObservableCollection<ToastItem> Notifications { get; } = new();

    public void ShowSuccess(string message) => AddToast(message, ToastType.Success, 3000);
    public void ShowInfo(string message) => AddToast(message, ToastType.Info, 4000);
    public void ShowWarning(string message) => AddToast(message, ToastType.Warning, 5000);
    public void ShowError(string message) => AddToast(message, ToastType.Error, 6000);

    public void DismissAll()
    {
        foreach (var toast in Notifications.ToList())
            toast.Dispose();
        Notifications.Clear();
    }

    [RelayCommand]
    private void Dismiss(ToastItem toast)
    {
        toast.Dispose();
        Notifications.Remove(toast);
    }

    private void AddToast(string message, ToastType type, int timeoutMs)
    {
        // Limit to 5 visible notifications
        while (Notifications.Count >= 5)
        {
            var oldest = Notifications[0];
            oldest.Dispose();
            Notifications.RemoveAt(0);
        }

        var toast = new ToastItem(message, type, timeoutMs, t =>
        {
            // Timer callback — remove from list
            Notifications.Remove(t);
        });

        Notifications.Add(toast);
    }
}

/// <summary>Type of toast notification, determines color and icon.</summary>
public enum ToastType { Success, Info, Warning, Error }

/// <summary>
/// Represents a single toast notification with auto-dismiss timer.
/// </summary>
public sealed class ToastItem : ObservableObject, IDisposable
{
    private readonly System.Timers.Timer _timer;
    private readonly Action<ToastItem> _onExpired;
    private bool _disposed;

    public string Message { get; }
    public ToastType Type { get; }

    public string Icon => Type switch
    {
        ToastType.Success => "\u2714",  // ✔
        ToastType.Info => "\u2139",     // ℹ
        ToastType.Warning => "\u26A0",  // ⚠
        ToastType.Error => "\u2716",    // ✖
        _ => "\u2139"
    };

    public ToastItem(string message, ToastType type, int timeoutMs, Action<ToastItem> onExpired)
    {
        Message = message;
        Type = type;
        _onExpired = onExpired;

        _timer = new System.Timers.Timer(timeoutMs);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = false;
        _timer.Start();
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!_disposed)
            _onExpired(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }
}
