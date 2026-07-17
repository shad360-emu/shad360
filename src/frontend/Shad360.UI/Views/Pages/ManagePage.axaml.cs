using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Shad360.ViewModels.Pages;

namespace Shad360.Views.Pages;

public partial class ManagePage : UserControl
{
    // Variables
    private ManagePageViewModel _viewModel { get; set; }
    
    // Constructor
    public ManagePage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ManagePageViewModel>();
        DataContext = _viewModel;
    }
}
