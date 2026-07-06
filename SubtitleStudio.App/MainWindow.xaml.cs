using System.Windows;
using SubtitleStudio.App.ViewModels;

namespace SubtitleStudio.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
