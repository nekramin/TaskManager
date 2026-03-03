using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace TaskManagerDesktop
{
    public class PriorityDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority priority)
            {
                return priority.DisplayName;
            }

            if (value is string priorityName)
            {
                // Для обратной совместимости
                return priorityName switch
                {
                    "Low" => "Низкий",
                    "Medium" => "Средний",
                    "High" => "Высокий",
                    "Critical" => "Критический",
                    _ => priorityName
                };
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string displayName)
            {
                // Находим соответствующий объект Priority
                var priorities = Priority.GetDefaultPriorities();
                return priorities.FirstOrDefault(p => p.DisplayName == displayName)
                    ?? priorities.First(p => p.Name == "Medium");
            }

            return value;
        }
    }
}