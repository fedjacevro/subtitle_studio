using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<SettingsViewModel>();
            DataContext = vm;
            await vm.RefreshModelStatusAsync();
        };
    }
}
