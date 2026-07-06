using System.Windows;

namespace SubtitleStudio.App.Helpers;

public class UserNotificationService
{
    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInfo(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}