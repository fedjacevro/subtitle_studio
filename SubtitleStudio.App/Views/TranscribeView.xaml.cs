using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class TranscribeView : UserControl
{
    public TranscribeView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<TranscribeViewModel>();
            DataContext = vm;
            vm.CheckModelStatus();
        };
    }
}
