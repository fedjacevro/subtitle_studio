using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class SourceView : UserControl
{
    public SourceView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<SourceViewModel>();
            DataContext = vm;
            await vm.InitializeAsync();
        };
    }
}
