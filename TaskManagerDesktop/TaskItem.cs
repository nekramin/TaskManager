using System;
using System.ComponentModel;
using System.Windows.Media;

namespace TaskManagerDesktop
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private DateTime _dueDate;
        private Priority _priority;
        private bool _isCompleted;
        private Category _category;
        private int _id;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public string CategoryName => Category?.Name ?? "Работа";
        public string CategoryIcon => Category?.IconCode ?? "🔧";
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

        public int PriorityOrder => Priority?.Level ?? 4;

        public SolidColorBrush DateColorBrush
        {
            get
            {
                Color color;
                if (IsCompleted) color = Colors.Green;
                else if (DueDate < DateTime.Now) color = Colors.Red;
                else if (DueDate < DateTime.Now.AddDays(1)) color = Colors.Orange;
                else color = Colors.Black;
                return new SolidColorBrush(color);
            }
        }

        public string StatusText
        {
            get
            {
                if (IsCompleted) return "Выполнена";
                if (DueDate < DateTime.Now) return "Просрочена";
                if (DueDate < DateTime.Now.AddDays(1)) return "Срочно";
                return "Активна";
            }
        }

        public SolidColorBrush StatusColorBrush
        {
            get
            {
                Color color;
                if (IsCompleted) color = Colors.Green;
                else if (DueDate < DateTime.Now) color = Colors.Red;
                else if (DueDate < DateTime.Now.AddDays(1)) color = Colors.Orange;
                else color = Colors.Blue;
                return new SolidColorBrush(color);
            }
        }

        public string GroupKey => IsCompleted ? "Выполненные" : "Активные";
        public string FormattedDate => DueDate.ToString("dd.MM.yyyy");
        public string FormattedTime => DueDate.ToString("HH:mm");

        public TaskItem()
        {
            Id = 0;
            Title = "Новая задача";
            Description = "Описание задачи";
            DueDate = DateTime.Now.AddDays(1);
            Priority = Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name == "Medium") ?? new Priority(3, "Medium", "Средний", 2, "#FF9800");
            IsCompleted = false;
            Category = Category.GetDefaultCategories().FirstOrDefault(c => c.Name == "Работа") ?? new Category(1, "Работа", "Рабочие задачи и проекты", "🔧", "#2196F3");
        }

        public TaskItem(string title, string description, DateTime dueDate, Priority priority, bool isCompleted)
            : this(title, description, dueDate, priority, isCompleted, Category.GetDefaultCategories().FirstOrDefault())
        { }

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

        public TaskItem Clone()
        {
            return new TaskItem
            {
                Id = this.Id,
                Title = this.Title,
                Description = this.Description,
                DueDate = this.DueDate,
                Priority = this.Priority,
                IsCompleted = this.IsCompleted,
                Category = this.Category
            };
        }

        public override string ToString()
        {
            return $"{Title} ({Priority?.DisplayName ?? PriorityName}) - [DueDate: {FormattedDate} {FormattedTime}] - {(IsCompleted ? "Выполнена" : "Активна")}, Категория: {Category?.Name ?? CategoryName}";
        }

        public static Priority[] GetAllPriorities()
        {
            return Priority.GetDefaultPriorities().ToArray();
        }

        public static string GetPriorityDisplayName(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));
            return priority?.DisplayName ?? "Неизвестно";
        }

        public static string GetPriorityDisplayName(Priority priority)
        {
            return priority?.DisplayName ?? "Неизвестно";
        }

        public static SolidColorBrush GetPriorityBrush(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));
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

        public string GetPriorityString() => Priority?.Name ?? "Medium";
        public string GetCategoryString() => Category?.Name ?? "Работа";

        public void SetPriorityFromString(string priorityName)
        {
            var priority = Priority.GetDefaultPriorities().FirstOrDefault(p => p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase));
            Priority = priority ?? Priority.GetDefaultPriorities().First(p => p.Name == "Medium");
        }

        public void SetCategoryFromString(string categoryName)
        {
            var category = Category.GetDefaultCategories().FirstOrDefault(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
            Category = category ?? Category.GetDefaultCategories().First(c => c.Name == "Работа");
        }

        public int PriorityId => Priority?.Id ?? 1004;
        public int? CategoryId => Category?.Id ?? 1002;
    }
}