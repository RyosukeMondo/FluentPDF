using Avalonia.Threading;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Avalonia implementation of the dispatcher service.
/// Provides UI thread dispatch operations using Avalonia's Dispatcher.
/// </summary>
public sealed class AvaloniaDispatcherService : IDispatcherService
{
    private readonly ILogger<AvaloniaDispatcherService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaDispatcherService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public AvaloniaDispatcherService(ILogger<AvaloniaDispatcherService>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsOnUIThread => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc/>
    public void Invoke(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsOnUIThread)
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Invoke(action);
        }
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (IsOnUIThread)
        {
            action();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(action);
        }
    }

    /// <inheritdoc/>
    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        if (IsOnUIThread)
        {
            return func();
        }
        else
        {
            return await Dispatcher.UIThread.InvokeAsync(func);
        }
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(Func<Task> asyncAction)
    {
        if (asyncAction == null)
        {
            throw new ArgumentNullException(nameof(asyncAction));
        }

        if (IsOnUIThread)
        {
            await asyncAction();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(asyncAction);
        }
    }

    /// <inheritdoc/>
    public void BeginInvoke(Action action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // Post action with low priority
        Dispatcher.UIThread.Post(action, DispatcherPriority.Background);
    }
}
