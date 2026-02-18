using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace TaskManagerDesktop
{
    public class TaskEditViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private TaskItem _currentTask;
        private TaskItem _originalTask;
        private string _windowTitle;
        private Priority _selectedPriority;
        private string _dueTime;
        private readonly INavigationService _navigationService;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Priority> Priorities { get; }
        public ObservableCollection<Category> Categories { get; }

        public TaskItem CurrentTask
        {
            get => _currentTask;
            set
            {
                _currentTask = value;
                OnPropertyChanged(nameof(CurrentTask));
            }
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string DueTime
        {
            get
            {
                if (string.IsNullOrEmpty(_dueTime) && CurrentTask != null)
                {
                    _dueTime = CurrentTask.DueDate.ToString("HH:mm");
                }
                return _dueTime;
            }
            set
            {
                if (_dueTime != value)
                {
                    _dueTime = value;
                    OnPropertyChanged(nameof(DueTime));
                    UpdateTaskTime(value);
                }
            }
        }

        public Priority SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority != value)
                {
                    _selectedPriority = value;
                    OnPropertyChanged(nameof(SelectedPriority));
                    if (CurrentTask != null && value != null)
                    {
                        CurrentTask.Priority = value;
                    }
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public bool DialogResult { get; private set; }

        public TaskEditViewModel(TaskItem task = null, INavigationService navigationService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
            Priorities = Priority.GetDefaultPriorities();
            Categories = Category.GetDefaultCategories();

            if (task != null)
            {
                CurrentTask = task.Clone();
                _originalTask = task;
                WindowTitle = "Редактирование задачи";
            }
            else
            {
                CurrentTask = new TaskItem();
                CurrentTask.DueDate = DateTime.Now.Date.AddHours(12);
                WindowTitle = "Новая задача";
            }

            SelectedPriority = CurrentTask.Priority ?? Priorities.FirstOrDefault(p => p.Name == "Medium");
            _dueTime = CurrentTask.DueDate.ToString("HH:mm");

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void UpdateTaskTime(string timeString)
        {
            if (CurrentTask != null && !string.IsNullOrEmpty(timeString))
            {
                if (DateTime.TryParseExact(timeString, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var time))
                {
                    var date = CurrentTask.DueDate.Date;
                    CurrentTask.DueDate = date.Add(time.TimeOfDay);
                }
            }
        }

        private void Save(object parameter)
        {
            UpdateTaskTime(DueTime);
            var validation = ModelHelper.ValidateTask(CurrentTask);
            if (!validation.isValid)
            {
                MessageBox.Show($"Ошибка валидации: {validation.errorMessage}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_originalTask != null)
            {
                _originalTask.Id = CurrentTask.Id;
                _originalTask.Title = CurrentTask.Title;
                _originalTask.Description = CurrentTask.Description;
                _originalTask.DueDate = CurrentTask.DueDate;
                _originalTask.Priority = CurrentTask.Priority;
                _originalTask.IsCompleted = CurrentTask.IsCompleted;
                _originalTask.Category = CurrentTask.Category;
            }

            DialogResult = true;
            CloseWindow();
        }

        private bool CanSave(object parameter)
        {
            bool isTimeValid = DateTime.TryParseExact(DueTime, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);
            var validation = ModelHelper.ValidateTask(CurrentTask);
            return !string.IsNullOrWhiteSpace(CurrentTask.Title) &&
                   CurrentTask.DueDate > DateTime.MinValue &&
                   SelectedPriority != null &&
                   isTimeValid &&
                   validation.isValid;
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
            CloseWindow();
        }

        private void CloseWindow()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = DialogResult;
                    window.Close();
                    break;
                }
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;
                switch (columnName)
                {
                    case nameof(CurrentTask.Title):
                        if (string.IsNullOrWhiteSpace(CurrentTask.Title))
                            error = "Название задачи обязательно";
                        else if (CurrentTask.Title.Length > 200)
                            error = "Название слишком длинное (макс. 200 символов)";
                        break;

                    case nameof(CurrentTask.DueDate):
                        if (CurrentTask.DueDate < DateTime.Now.AddMinutes(-5))
                            error = "Дата выполнения не может быть в прошлом";
                        break;

                    case nameof(DueTime):
                        if (string.IsNullOrEmpty(DueTime))
                            error = "Время выполнения обязательно";
                        else if (!DateTime.TryParseExact(DueTime, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _))
                            error = "Введите время в формате ЧЧ:ММ (24-часовой), например: 14:30";
                        break;
                }
                return error;
            }
        }

        public string Error => string.Empty;
    

    protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

        public void Execute(object parameter) => _execute(parameter);
    }
}