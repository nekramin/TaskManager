using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManagerDesktop
{
    /// <summary>
    /// Класс для представления категории задачи
    /// </summary>
    public class Category : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _description;
        private string _iconCode;
        private string _colorCode;

        // Событие для уведомления об изменении свойств
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Идентификатор категории (первичный ключ в БД)
        /// </summary>
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

        /// <summary>
        /// Название категории
        /// </summary>
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

        /// <summary>
        /// Описание категории
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Код иконки для категории (эмодзи или код шрифта)
        /// </summary>
        public string IconCode
        {
            get => _iconCode;
            set
            {
                if (_iconCode != value)
                {
                    _iconCode = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Цвет для отображения категории (HEX)
        /// </summary>
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

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Category()
        {
            Name = "Работа";
            Description = "Рабочие задачи и проекты";
            IconCode = "💼";
            ColorCode = "#2196F3"; // Blue
        }

        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        public Category(int id, string name, string description, string iconCode, string colorCode)
        {
            Id = id;
            Name = name;
            Description = description;
            IconCode = iconCode;
            ColorCode = colorCode;
        }

        /// <summary>
        /// Метод для вызова события изменения свойства
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Создает коллекцию стандартных категорий
        /// </summary>
        public static System.Collections.ObjectModel.ObservableCollection<Category> GetDefaultCategories()
        {
            return new System.Collections.ObjectModel.ObservableCollection<Category>
            {
                new Category(1, "Работа", "Рабочие задачи и проекты", "💼", "#2196F3"), // Blue
                new Category(2, "Личное", "Личные дела и хобби", "🏠", "#4CAF50"), // Green
                new Category(3, "Учеба", "Образовательные задачи", "📚", "#9C27B0"), // Purple
                new Category(4, "Здоровье", "Здоровье и спорт", "🏥", "#FF9800"), // Orange
                new Category(5, "Финансы", "Финансовые вопросы", "💰", "#FFC107"), // Amber
                new Category(6, "Семья", "Семейные дела", "👨👧", "#E91E63"), // Pink
                new Category(7, "Отдых", "Отдых и развлечения", "🎮", "#00BCD4") // Cyan
            };
        }

        /// <summary>
        /// Переопределение ToString для отладки
        /// </summary>
        public override string ToString()
        {
            return $"{IconCode} {Name} - {Description}";
        }
    }
}