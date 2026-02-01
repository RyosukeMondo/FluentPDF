namespace FluentPDF.Core.Services;

/// <summary>
/// Abstraction for UI thread dispatcher operations.
/// Allows ViewModels to dispatch operations to the UI thread without direct framework dependencies.
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// Gets a value indicating whether the current thread is the UI thread.
    /// </summary>
    bool IsOnUIThread { get; }

    /// <summary>
    /// Invokes an action on the UI thread.
    /// If already on the UI thread, executes immediately.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    void Invoke(Action action);

    /// <summary>
    /// Invokes an action on the UI thread asynchronously.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <returns>A task that completes when the action has been executed.</returns>
    Task InvokeAsync(Action action);

    /// <summary>
    /// Invokes a function on the UI thread asynchronously and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to invoke.</param>
    /// <returns>A task containing the result of the function.</returns>
    Task<T> InvokeAsync<T>(Func<T> func);

    /// <summary>
    /// Invokes an async function on the UI thread.
    /// </summary>
    /// <param name="asyncAction">The async action to invoke.</param>
    /// <returns>A task that completes when the async action has completed.</returns>
    Task InvokeAsync(Func<Task> asyncAction);

    /// <summary>
    /// Schedules an action to be executed on the UI thread with low priority.
    /// Useful for non-urgent UI updates that should not block user interactions.
    /// </summary>
    /// <param name="action">The action to schedule.</param>
    void BeginInvoke(Action action);
}
