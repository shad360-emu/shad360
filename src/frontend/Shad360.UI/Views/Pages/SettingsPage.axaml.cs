using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Shad360.ViewModels.Pages;

namespace Shad360.Views.Pages;

public partial class SettingsPage : UserControl
{
    // Variables
    private SettingsPageViewModel _viewModel { get; set; }

    // Constructor
    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsPageViewModel>();
        DataContext = _viewModel;
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // Refresh settings to ensure the UI reflects current values
        _viewModel.RefreshSettings();
    }
}
