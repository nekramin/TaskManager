using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TaskManagerDesktop
{
    /// <summary>
    /// Класс для представления задачи в приложении TaskManager
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        // Поля для хранения данных
        private string _title;
        private string _description;
        private DateTime _dueDate;
        private Priority _priority;  // Priority
        private bool _isCompleted;
        private Category _category;  // Category
        private int _id; // Идентификатор для БД

        // Событие для уведомления об изменении свойств
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод для вызова события изменения свойства
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Название задачи
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        // Описание задачи
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        // Дата и время выполнения задачи
        public DateTime DueDate
        {
            get => _dueDate;
            set
            {
                if (_dueDate != value)
                {
                    _dueDate = value;
                    OnPropertyChanged(nameof(DueDate));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColorBrush));
                    OnPropertyChanged(nameof(DateColorBrush));
                }
            }
        }

        // Приоритет задачи (Low, Medium, High, Critical)
        public Priority Priority
        {
            get => _priority;
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    OnPropertyChanged(nameof(Priority));
                    OnPropertyChanged(nameof(PriorityColorBrush));
                    OnPropertyChanged(nameof(PrioritySymbol));
                    OnPropertyChanged(nameof(PriorityOrder));
                    OnPropertyChanged(nameof(PriorityName));
                }
            }
        }

        public string PriorityName => Priority?.Name ?? "Medium";

        // Флаг выполнения задачи
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColorBrush));
                    OnPropertyChanged(nameof(DateColorBrush));
                    OnPropertyChanged(nameof(GroupKey));
                }
            }
        }

        // Категория задачи
        public Category Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged(nameof(Category));
                    OnPropertyChanged(nameof(CategoryName));
                    OnPropertyChanged(nameof(CategoryIcon));
                    OnPropertyChanged(nameof(CategoryColorBrush));
                }
            }
        }

        // Вспомогательное свойство для получения имени категории
        public string CategoryName => Category?.Name ?? "Работа";

        // Вспомогательное свойство для получения иконки категории
        public string CategoryIcon => Category?.IconCode ?? "📋";

        // Кисть для цвета категории
        public SolidColorBrush CategoryColorBrush
        {
            get
            {
                if (Category?.ColorCode != null)
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(Category.ColorCode);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        return new SolidColorBrush(Colors.Gray);
                    }
                }
                return new SolidColorBrush(Colors.Gray);
            }
        }

        // Идентификатор задачи (для БД)
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        // Кисть для отображения цвета приоритета
        public SolidColorBrush PriorityColorBrush
        {
            get
            {
                if (Priority?.ColorCode != null)
                {
                    try
                    {
                        var color = (Color)ColorConverter.ConvertFromString(Priority.ColorCode);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        // Если не удалось преобразовать цвет, используем старую логику
                        return PriorityName switch
                        {
                            "Low" => new SolidColorBrush(Colors.LightGreen),
                            "Medium" => new SolidColorBrush(Colors.Orange),
                            "High" => new SolidColorBrush(Colors.OrangeRed),
                            "Critical" => new SolidColorBrush(Colors.Red),
                            _ => new SolidColorBrush(Colors.Gray)
                        };
                    }
                }

                // Запасной вариант для совместимости
                return PriorityName switch
                {
                    "Low" => new SolidColorBrush(Colors.LightGreen),
                    "Medium" => new SolidColorBrush(Colors.Orange),
                    "High" => new SolidColorBrush(Colors.OrangeRed),
                    "Critical" => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
        }

        // Символ приоритета для отображения в кружке
        public string PrioritySymbol
        {
            get
            {
                if (Priority == null) return "?";

                return Priority.Name switch
                {
                    "Low" => "L",
                    "Medium" => "M",
                    "High" => "H",
                    "Critical" => "C",
                    _ => "?"
                };
            }
        }

        // Числовое значение приоритета для сортировки
        public int PriorityOrder
        {
            get
            {
                if (Priority == null) return 4;
                return Priority.Level; // Используем Level из объекта Priority
            }
        }

        // Кисть для цвета даты (зависит от статуса и срока)
        public SolidColorBrush DateColorBrush
        {
            get
            {
                Color color;
                if (IsCompleted)
                    color = Colors.Green;
                else if (DueDate < DateTime.Now)
                    color = Colors.Red;
                else if (DueDate < DateTime.Now.AddDays(1))
                    color = Colors.Orange;
                else
                    color = Colors.Black;

                return new SolidColorBrush(color);
            }
        }

        // Текстовое представление статуса задачи
        public string StatusText
        {
            get
            {
                if (IsCompleted)
                    return "Выполнена";
                if (DueDate < DateTime.Now)
                    return "Просрочена";
                if (DueDate < DateTime.Now.AddDays(1))
                    return "Срочно";
                return "Активна";
            }
        }

        // Кисть для цвета статуса задачи
        public SolidColorBrush StatusColorBrush
        {
            get
            {
                Color color;
                if (IsCompleted)
                    color = Colors.Green;
                else if (DueDate < DateTime.Now)
                    color = Colors.Red;
                else if (DueDate < DateTime.Now.AddDays(1))
                    color = Colors.Orange;
                else
                    color = Colors.Blue;

                return new SolidColorBrush(color);
            }
        }

        // Ключ для группировки задач (Выполненные/Активные)
        public string GroupKey => IsCompleted ? "Выполненные" : "Активные";

        // Форматированная дата для отображения (только дата)
        public string FormattedDate => DueDate.ToString("dd.MM.yyyy");

        // Форматированное время для отображения
        public string FormattedTime => DueDate.ToString("HH:mm");

        // Конструктор по умолчанию
        public TaskItem()
        {
            Id = 0;
            Title = "Новая задача";
            Description = "Описание задачи";
            DueDate = DateTime.Now.AddDays(1);

            Priority = Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name == "Medium")
                ?? new Priority(3, "Medium", "Средний", 2, "#FF9800");

            IsCompleted = false;

            Category = Category.GetDefaultCategories().FirstOrDefault(c => c.Name == "Работа")
                ?? new Category(1, "Работа", "Рабочие задачи и проекты", "💼", "#2196F3");
        }

        // Конструктор с параметрами
        public TaskItem(string title, string description, DateTime dueDate, Priority priority, bool isCompleted)
            : this(title, description, dueDate, priority, isCompleted,
                Category.GetDefaultCategories().FirstOrDefault(c => c.Name == "Работа"))
        {
        }

        // Конструктор с параметрами (включая категорию)
        public TaskItem(string title, string description, DateTime dueDate, Priority priority, bool isCompleted, Category category)
        {
            Id = 0;
            Title = title;
            Description = description;
            DueDate = dueDate;
            Priority = priority ?? Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name == "Medium");
            IsCompleted = isCompleted;
            Category = category ?? Category.GetDefaultCategories().FirstOrDefault(c => c.Name == "Работа");
        }

        // Метод для клонирования задачи
        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                DueDate = this.DueDate,
                Priority = this.Priority,  // Теперь копируем объект, а не строку
                IsCompleted = this.IsCompleted,
                Category = this.Category   // Теперь копируем объект, а не строку
            };
        }

        // Переопределение ToString для отладки
        public override string ToString()
        {
            return $"{Title} ({Priority?.DisplayName ?? PriorityName}) - " +
                   $"{DueDate:dd.MM.yyyy HH:mm} - " +
                   $"{(IsCompleted ? "Выполнена" : "Активна")} - " +
                   $"Категория: {Category?.Name ?? CategoryName}";
        }

        // Возвращает список всех доступных приоритетов
        public static Priority[] GetAllPriorities()
        {
            return Priority.GetDefaultPriorities().ToArray();
        }

        // Возвращает отображаемое имя приоритета
        public static string GetPriorityDisplayName(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities()
                .FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));

            return priority?.DisplayName ?? "Неизвестно";
        }

        // Перегрузка для объекта Priority
        public static string GetPriorityDisplayName(Priority priority)
        {
            return priority?.DisplayName ?? "Неизвестно";
        }

        // Возвращает кисть для заданного приоритета
        public static SolidColorBrush GetPriorityBrush(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities()
                .FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));

            if (priority?.ColorCode != null)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(priority.ColorCode);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        // Перегрузка для объекта Priority
        public static SolidColorBrush GetPriorityBrush(Priority priority)
        {
            if (priority?.ColorCode != null)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(priority.ColorCode);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }

            return new SolidColorBrush(Colors.Gray);
        }

        // Для получения строкового имени приоритета
        public string GetPriorityString() => Priority?.Name ?? "Medium";

        // Для получения строкового имени категории
        public string GetCategoryString() => Category?.Name ?? "Работа";

        // Для установки приоритета по строке
        public void SetPriorityFromString(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities()
                .FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));

            Priority = priority ?? Priority.GetDefaultPriorities().First(p => p.Name == "Medium");
        }

        // Для установки категории по строке
        public void SetCategoryFromString(string categoryName)
        {
            var category = Category.GetDefaultCategories()
                .FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));

            Category = category ?? Category.GetDefaultCategories().First(c => c.Name == "Работа");
        }

        public int PriorityId => Priority?.Id ?? 1004;
        public int? CategoryId => Category?.Id ?? 1002;
    }
}