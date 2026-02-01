// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace FluentPDF.App.Views;

/// <summary>
/// Full-screen presentation window for PDF slideshow mode.
/// </summary>
public sealed partial class PresentationWindow : Window
{
    /// <summary>
    /// Gets the current active presentation window instance.
    /// </summary>
    public static PresentationWindow? CurrentInstance { get; private set; }

    public PresentationWindow()
    {
        InitializeComponent();
        CurrentInstance = this;
    }

    public PresentationWindow(PresentationViewModel viewModel, ILogger<PresentationWindow> logger)
    {
        InitializeComponent();
        // Store viewModel for later use if needed
        CurrentInstance = this;
    }
}
