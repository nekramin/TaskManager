using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskManagerDesktop
{
    public class PriorityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string priority)
            {
                return priority switch
                {
                    "Low" => Brushes.LightGreen,
                    "Medium" => Brushes.Orange,
                    "High" => Brushes.OrangeRed,
                    "Critical" => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}