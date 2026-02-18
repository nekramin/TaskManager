using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManagerDesktop
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorCode)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(colorCode);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return Brushes.Gray;
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}