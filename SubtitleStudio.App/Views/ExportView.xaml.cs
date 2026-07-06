using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class ExportView : UserControl
{
    public ExportView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<ExportViewModel>();
            DataContext = vm;
        };
    }
}
