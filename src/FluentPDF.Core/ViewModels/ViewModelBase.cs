using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Uses CommunityToolkit.Mvvm for cross-platform MVVM support.
/// This class is UI-framework agnostic and can be used with WinUI, Avalonia, or other frameworks.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Gets or sets a value indicating whether the view model is busy with an operation.
    /// </summary>
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        protected set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the view model has been initialized.
    /// </summary>
    private bool _isInitialized;
    public bool IsInitialized
    {
        get => _isInitialized;
        protected set => SetProperty(ref _isInitialized, value);
    }

    /// <summary>
    /// Initializes the view model asynchronously.
    /// Override this method to perform async initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up resources when the view model is being disposed.
    /// Override this method to perform cleanup logic.
    /// </summary>
    public virtual void Cleanup()
    {
    }
}
