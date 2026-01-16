using System.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Services;

/// <summary>
/// Verifies that UI bindings successfully update after ViewModel property changes.
/// Detects cases where PropertyChanged events fire but UI controls don't reflect the change.
/// </summary>
public sealed class UIBindingVerifier
{
    /// <summary>
    /// Verifies that a PropertyChanged event is raised for a specific property within a timeout period.
    /// </summary>
    /// <typeparam name="T">Type of the ViewModel (must implement INotifyPropertyChanged).</typeparam>
    /// <param name="viewModel">The ViewModel to monitor.</param>
    /// <param name="propertyName">Name of the property to watch for changes.</param>
    /// <param name="timeout">Maximum time to wait for the PropertyChanged event.</param>
    /// <returns>True if PropertyChanged event fired for the specified property within timeout; otherwise false.</returns>
    /// <example>
    /// <code>
    /// var success = await verifier.VerifyPropertyUpdateAsync(viewModel, nameof(CurrentPageImage), TimeSpan.FromMilliseconds(500));
    /// if (!success)
    /// {
    ///     // Handle binding failure
    /// }
    /// </code>
    /// </example>
    public Task<bool> VerifyPropertyUpdateAsync<T>(T viewModel, string propertyName, TimeSpan timeout)
        where T : INotifyPropertyChanged
    {
        if (viewModel == null)
            throw new ArgumentNullException(nameof(viewModel));
        if (string.IsNullOrEmpty(propertyName))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(propertyName));

        var tcs = new TaskCompletionSource<bool>();
        PropertyChangedEventHandler? handler = null;
        CancellationTokenSource? cts = null;

        handler = (sender, args) =>
        {
            if (args.PropertyName == propertyName)
            {
                // Event fired for the target property - success
                viewModel.PropertyChanged -= handler;
                cts?.Cancel();
                tcs.TrySetResult(true);
            }
        };

        // Subscribe to PropertyChanged event
        viewModel.PropertyChanged += handler;

        // Set up timeout
        cts = new CancellationTokenSource();
        _ = Task.Delay(timeout, cts.Token).ContinueWith(task =>
        {
            if (!task.IsCanceled)
            {
                // Timeout expired - cleanup and return false
                viewModel.PropertyChanged -= handler;
                tcs.TrySetResult(false);
            }
        }, TaskScheduler.Default);

        return tcs.Task;
    }

    /// <summary>
    /// Verifies that an Image control's Source property becomes non-null within a timeout period.
    /// Polls the control on the UI thread at regular intervals.
    /// </summary>
    /// <param name="control">The Image control to monitor.</param>
    /// <param name="timeout">Maximum time to wait for Source to become non-null.</param>
    /// <returns>True if Image.Source becomes non-null within timeout; otherwise false.</returns>
    /// <remarks>
    /// This method is useful for detecting cases where PropertyChanged events fire but
    /// the WinUI binding system fails to update the UI control.
    /// </remarks>
    /// <example>
    /// <code>
    /// var imageControl = FindImageControl();
    /// var success = await verifier.VerifyImageControlUpdateAsync(imageControl, TimeSpan.FromSeconds(5));
    /// if (!success)
    /// {
    ///     // Image control didn't update - potential binding failure
    /// }
    /// </code>
    /// </example>
    public Task<bool> VerifyImageControlUpdateAsync(Image control, TimeSpan timeout)
    {
        if (control == null)
            throw new ArgumentNullException(nameof(control));

        var tcs = new TaskCompletionSource<bool>();
        var startTime = DateTime.UtcNow;
        var pollInterval = TimeSpan.FromMilliseconds(100);

        // Get the DispatcherQueue for the control's UI thread
        var dispatcherQueue = control.DispatcherQueue;
        if (dispatcherQueue == null)
        {
            // Control not associated with a dispatcher - can't verify
            tcs.SetResult(false);
            return tcs.Task;
        }

        void PollControl()
        {
            var elapsed = DateTime.UtcNow - startTime;
            if (elapsed >= timeout)
            {
                // Timeout expired - Source never became non-null
                tcs.TrySetResult(false);
                return;
            }

            // Check control.Source on UI thread
            var enqueued = dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (control.Source != null)
                    {
                        // Success - Source is non-null
                        tcs.TrySetResult(true);
                    }
                    else
                    {
                        // Source still null - poll again after interval
                        _ = Task.Delay(pollInterval).ContinueWith(_ => PollControl(), TaskScheduler.Default);
                    }
                }
                catch (Exception)
                {
                    // Control may have been disposed or other error - treat as failure
                    tcs.TrySetResult(false);
                }
            });

            if (!enqueued)
            {
                // Failed to enqueue - dispatcher may be shutting down
                tcs.TrySetResult(false);
            }
        }

        // Start polling
        PollControl();

        return tcs.Task;
    }
}
