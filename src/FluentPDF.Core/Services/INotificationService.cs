namespace FluentPDF.Core.Services;

/// <summary>
/// Lightweight, non-blocking notification service for toast/snackbar messages.
/// Unlike IDialogService (modal, blocking), these auto-dismiss after a timeout.
/// </summary>
public interface INotificationService
{
    /// <summary>Shows a success toast (green, auto-dismiss 3s).</summary>
    void ShowSuccess(string message);

    /// <summary>Shows an info toast (blue, auto-dismiss 4s).</summary>
    void ShowInfo(string message);

    /// <summary>Shows a warning toast (orange, auto-dismiss 5s).</summary>
    void ShowWarning(string message);

    /// <summary>Shows an error toast (red, auto-dismiss 6s, with dismiss button).</summary>
    void ShowError(string message);

    /// <summary>Dismisses all active notifications.</summary>
    void DismissAll();
}
