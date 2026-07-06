using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App.Views;

public partial class EditSubtitlesView : UserControl
{
    public EditSubtitlesView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var vm = App.ServiceProvider!.GetRequiredService<EditSubtitlesViewModel>();
            DataContext = vm;
        };
    }
}
