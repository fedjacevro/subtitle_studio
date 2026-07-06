using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SubtitleStudio.App.Converters;

public class StepToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out var step))
            return currentStep == step ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StepToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out var step))
        {
            if (currentStep == step)
                return new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x4A));
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StepToBorderConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentStep && parameter is string stepStr && int.TryParse(stepStr, out var step))
        {
            if (currentStep == step)
                return new SolidColorBrush(Color.FromRgb(0x7C, 0x4D, 0xFF));
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && parameter is string format)
            return string.Format(format, s);
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
