using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManagerDesktop
{
    public class Category : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _description;
        private string _iconCode;
        private string _colorCode;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public Category()
        {
            Name = "Работа";
            Description = "Рабочие задачи и проекты";
            IconCode = "🔧";
            ColorCode = "#2196F3";
        }

        public Category(int id, string name, string description, string iconCode, string colorCode)
        {
            Id = id;
            Name = name;
            Description = description;
            IconCode = iconCode;
            ColorCode = colorCode;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static System.Collections.ObjectModel.ObservableCollection<Category> GetDefaultCategories()
        {
            return new System.Collections.ObjectModel.ObservableCollection<Category>
            {
                new Category(1, "Работа", "Рабочие задачи и проекты", "🔧", "#2196F3"),
                new Category(2, "Личное", "Личные дела и хобби", "🎨", "#4CAF50"),
                new Category(3, "Учеба", "Образовательные задачи", "📚", "#9C27B0"),
                new Category(4, "Здоровье", "Здоровье и спорт", "💊", "#FF9800"),
                new Category(5, "Финансы", "Финансовые вопросы", "💰", "#FFC107"),
                new Category(6, "Семья", "Семейные дела", "👪", "#E91E63"),
                new Category(7, "Отдых", "Отдых и развлечения", "🏖️", "#00BCD4")
            };
        }

        public override string ToString()
        {
            return $"{IconCode} ({Name}) - [{Description}]";
        }
    }
}