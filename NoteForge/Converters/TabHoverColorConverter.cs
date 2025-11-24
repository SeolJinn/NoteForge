using System.Globalization;

namespace NoteForge.Converters;

public class TabHoverColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            if (Application.Current!.Resources.TryGetValue("AppBackground", out var activeColor))
            {
                return activeColor;
            }
        }

        if (Application.Current!.Resources.TryGetValue("AppSurface", out var hoverColor))
        {
            return hoverColor;
        }

        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

