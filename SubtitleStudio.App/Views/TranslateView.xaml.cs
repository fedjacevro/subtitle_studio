using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class TranslateView : UserControl
{
    public TranslateView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<TranslateViewModel>();
            DataContext = vm;
            await vm.InitializeAsync();
        };
    }
}
