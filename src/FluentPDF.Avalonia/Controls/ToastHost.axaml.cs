using Avalonia.Controls;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Host control for toast notifications. Place at the root of the window
/// overlaying other content. Binds to NotificationViewModel.
/// </summary>
public partial class ToastHost : UserControl
{
    public ToastHost()
    {
        InitializeComponent();
    }
}
