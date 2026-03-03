using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManagerDesktop
{
    /// Класс для представления приоритета задачи
    public class Priority : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _displayName;
        private int _level;
        private string _colorCode;

        /// Событие для уведомления об изменении свойств
        public event PropertyChangedEventHandler PropertyChanged;

        /// Идентификатор приоритета (первичный ключ в БД)
        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// Системное имя приоритета (англ.)
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// Отображаемое имя приоритета
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// Уровень приоритета (0 - самый высокий)
        public int Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PriorityOrder));
                }
            }
        }

        /// Цвет для отображения приоритета (HEX)
        public string ColorCode
        {
            get => _colorCode;
            set
            {
                if (_colorCode != value)
                {
                    _colorCode = value;
                    OnPropertyChanged();
                }
            }
        }

        /// Свойство для сортировки (аналогично PriorityOrder в TaskItem)
        public int PriorityOrder => Level;

        /// Конструктор по умолчанию
        public Priority()
        {
            Name = "Medium";
            DisplayName = "Средний";
            Level = 2;
            ColorCode = "#FF9800"; // Orange
        }

        /// Конструктор с параметрами
        public Priority(int id, string name, string displayName, int level, string colorCode)
        {
            Id = id;
            Name = name;
            DisplayName = displayName;
            Level = level;
            ColorCode = colorCode;
        }

        /// Метод для вызова события изменения свойства
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// Создает коллекцию стандартных приоритетов
        public static System.Collections.ObjectModel.ObservableCollection<Priority> GetDefaultPriorities()
        {
            return new System.Collections.ObjectModel.ObservableCollection<Priority>
            {
                new Priority(1, "Critical", "Критический", 0, "#F44336"), // Red
                new Priority(2, "High", "Высокий", 1, "#FF5722"), // Deep Orange
                new Priority(3, "Medium", "Средний", 2, "#FF9800"), // Orange
                new Priority(4, "Low", "Низкий", 3, "#4CAF50") // Green
            };
        }

        /// Переопределение ToString для отладки
        public override string ToString()
        {
            return $"{DisplayName} ({Name}) - Уровень: {Level}";
        }
    }
}