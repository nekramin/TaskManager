using System;
using System.Globalization;
using System.Windows.Data;

namespace TaskManagerDesktop
{
    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd.MM.yyyy HH:mm");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && DateTime.TryParse(str, out DateTime result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }
}
