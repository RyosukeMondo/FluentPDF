// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Views;

/// <summary>
/// Dialog for encrypting PDF documents.
/// </summary>
public sealed partial class EncryptDialog : ContentDialog
{
    public EncryptDialog()
    {
        InitializeComponent();
    }

    public EncryptDialog(EncryptDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
