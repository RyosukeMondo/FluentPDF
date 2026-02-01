// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Core.Services;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Views;

/// <summary>
/// Dialog for exporting PDF pages as images.
/// </summary>
public sealed partial class ExportImagesDialog : ContentDialog
{
    public string OutputFolder { get; set; } = string.Empty;

    public ExportImagesDialog()
    {
        InitializeComponent();
    }

    public ImageExportOptions GetExportOptions()
    {
        return new ImageExportOptions
        {
            Format = ImageFormat.Png,
            Dpi = 300,
            JpegQuality = 90,
            PageRange = "all"
        };
    }
}
