using System;
using Microsoft.UI; // Wichtig für Colors
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI; // Wichtig für die Color-Struktur

namespace MyUnoApp;

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOnline)
        {
            // Online: Leichtes Grün (Dunkles Waldgrün)
            // Offline: Dunkles Anthrazit
            Color selectedColor = isOnline 
                ? Color.FromArgb(255, 26, 46, 26) 
                : Color.FromArgb(255, 30, 30, 30);

            return new SolidColorBrush(selectedColor);
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) 
        => throw new NotImplementedException();
}
