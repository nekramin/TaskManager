using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManagerDesktop
{
    public class Priority : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _displayName;
        private int _level;
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

        public int PriorityOrder => Level;

        public Priority()
        {
            Name = "Medium";
            DisplayName = "Средний";
            Level = 2;
            ColorCode = "#FF9800";
        }

        public Priority(int id, string name, string displayName, int level, string colorCode)
        {
            Id = id;
            Name = name;
            DisplayName = displayName;
            Level = level;
            ColorCode = colorCode;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static System.Collections.ObjectModel.ObservableCollection<Priority> GetDefaultPriorities()
        {
            return new System.Collections.ObjectModel.ObservableCollection<Priority>
            {
                new Priority(1, "Critical", "Критический", 0, "#F44336"),
                new Priority(2, "High", "Высокий", 1, "#FF5722"),
                new Priority(3, "Medium", "Средний", 2, "#FF9800"),
                new Priority(4, "Low", "Низкий", 3, "#4CAF50")
            };
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Name}) - Уровень: {Level}";
        }
    }
}