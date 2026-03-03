using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using TaskManagerDesktop.Services;

namespace TaskManagerDesktop
{
    public class TaskEditViewModel : INotifyPropertyChanged
    {
        private TaskItem _currentTask;
        private TaskItem _originalTask;
        private string _windowTitle;
        private string _dueTime;
        private Priority _selectedPriority;
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private string _statusMessage;
        private bool _isSaving;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Priority> Priorities { get; set; }
        public ObservableCollection<Category> Categories { get; set; }

        public TaskEditViewModel(TaskItem task = null, INavigationService navigationService = null, IDataService dataService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
            _dataService = dataService ?? new DataService();

            // Инициализируем коллекций
            Priorities = new ObservableCollection<Priority>(Priority.GetDefaultPriorities());
            Categories = new ObservableCollection<Category>(Category.GetDefaultCategories());

            _ = LoadReferenceDataAsync();

            // Если задача передана - редактируем, иначе создаём новую
            if (task != null)
            {
                CurrentTask = task.Clone();
                _originalTask = task;
                WindowTitle = "Редактирование задачи";
            }
            else
            {
                CurrentTask = new TaskItem();
                // Устанавливаем время по умолчанию (12:00)
                CurrentTask.DueDate = DateTime.Now.Date.AddHours(12);
                WindowTitle = "Новая задача";
            }

            // Устанавливаем выбранные значения из задачи
            SelectedPriority = CurrentTask.Priority ??
                               Priorities.FirstOrDefault(p => p.Name == "Medium");

            // Инициализируем время
            _dueTime = CurrentTask.DueDate.ToString("HH:mm");

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            // 8. Инициализируем статус
            StatusMessage = "Готово к работе";
        }

        private async Task LoadReferenceDataAsync()
        {
            try
            {
                // Загружаем приоритеты и категории из БД
                var prioritiesFromDb = await ReferenceDataCache.GetPrioritiesAsync();
                var categoriesFromDb = await ReferenceDataCache.GetCategoriesAsync();

                // Обновляем коллекции в UI-потоке
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Priorities.Clear();
                    foreach (var priority in prioritiesFromDb)
                    {
                        Priorities.Add(priority);
                    }

                    Categories.Clear();
                    foreach (var category in categoriesFromDb)
                    {
                        Categories.Add(category);
                    }

                    // После обновления списков убедимся, что выбранный приоритет все еще валиден
                    if (SelectedPriority != null)
                    {
                        var priorityExists = Priorities.Any(p => p.Id == SelectedPriority.Id);
                        if (!priorityExists)
                        {
                            SelectedPriority = Priorities.FirstOrDefault(p => p.Name == "Medium");
                            if (SelectedPriority != null)
                                CurrentTask.Priority = SelectedPriority;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // В случае ошибки просто оставляем тестовые данные
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Не удалось загрузить справочные данные, используются локальные.";
                });
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки справочных данных: {ex}");
            }
        }

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

        // Свойство для времени выполнения
        public string DueTime
        {
            get
            {
                // Если время еще не установлено, берем из CurrentTask
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

                    // Обновляем время в CurrentTask
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

                    // Обновляем Priority в CurrentTask
                    if (CurrentTask != null && value != null)
                    {
                        CurrentTask.Priority = value;
                    }
                }
            }
        }

        // Свойства для статуса
        // Ссылок: 3
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        // Ссылок: 5
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged(nameof(IsSaving));

                    // Переоцениваем возможность выполнения команды Save
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Команды
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Флаг, указывающий на успешное сохранение
        public bool DialogResult { get; private set; }

        private void UpdateTaskTime(string timeString)
        {
            if (CurrentTask != null && !string.IsNullOrEmpty(timeString))
            {
                if (DateTime.TryParseExact(timeString, "HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var time))
                {
                    var date = CurrentTask.DueDate.Date; // Берем только дату
                    CurrentTask.DueDate = date.Add(time.TimeOfDay); // Добавляем время
                }
            }
        }

        private async void Save(object parameter)
        {
            // Предотвращаем повторное сохранение
            if (IsSaving)
            {
                System.Diagnostics.Debug.WriteLine("Сохранение уже выполняется, повторный вызов игнорируется.");
                return;
            }

            try
            {
                // Устанавливаем флаг сохранения (блокирует кнопку)
                IsSaving = true;
                StatusMessage = "Сохранение задачи...";

                // Обновляем время из поля DueTime
                if (!string.IsNullOrEmpty(DueTime))
                {
                    UpdateTaskTime(DueTime);
                }

                // Валидация задачи
                var validation = ModelHelper.ValidateTask(CurrentTask);
                if (!validation.isValid)
                {
                    StatusMessage = $"Ошибка валидации: {validation.errorMessage}";
                    MessageBox.Show(
                        $"Проверьте правильность заполнения полей:\n\n{validation.errorMessage}",
                        "Ошибка валидации",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Сохранение в базу данных
                if (_originalTask == null) // Режим: Новая задача
                {
                    StatusMessage = "Добавление новой задачи в базу данных...";

                    // Вызываем метод репозитория через DataService
                    var savedTask = await _dataService.SaveTaskAsync(CurrentTask);

                    // Обновляем CurrentTask, чтобы он содержал ID из БД
                    CurrentTask.Id = savedTask.Id;

                    // Обновляем статус
                    StatusMessage = $"Задача успешно добавлена (ID: {savedTask.Id})";

                    // Устанавливаем положительный результат диалога
                    DialogResult = true;
                }
                else // Редактирование существующей задачи
                {
                    StatusMessage = "Обновление задачи в базе данных...";

                    // Копируем изменения из CurrentTask в _originalTask
                    _originalTask.Title = CurrentTask.Title;
                    _originalTask.Description = CurrentTask.Description;
                    _originalTask.DueDate = CurrentTask.DueDate;
                    _originalTask.Priority = CurrentTask.Priority;
                    _originalTask.IsCompleted = CurrentTask.IsCompleted;
                    _originalTask.Category = CurrentTask.Category;

                    // Вызываем метод обновления
                    bool updateResult = await _dataService.UpdateTaskAsync(_originalTask);

                    if (updateResult)
                    {
                        StatusMessage = $"Задача (ID: {_originalTask.Id}) успешно обновлена";
                        DialogResult = true;
                    }
                    else
                    {
                        throw new Exception("База данных не подтвердила обновление записи.");
                    }
                }

                // Закрываем окно, если всё прошло успешно
                if (DialogResult)
                {
                    // Небольшая задержка, чтобы пользователь увидел статус
                    await Task.Delay(300);
                    CloseWindow();
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок

                // Логируем в Output window Visual Studio
                System.Diagnostics.Debug.WriteLine($"=== ОШИБКА СОХРАНЕНИЯ ЗАДАЧИ ===");
                System.Diagnostics.Debug.WriteLine($"Сообщение: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Тип: {ex.GetType().Name}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"Inner: {ex.InnerException.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                // Формируем понятное сообщение для пользователя
                string userMessage = "Не удалось сохранить задачу.\n\n";

                if (ex.Message.Contains("ForeignKey") || ex.Message.Contains("REFERENCE"))
                {
                    userMessage += "Ошибка ссылочной целостности.\n" +
                                   "Убедитесь, что выбранный приоритет и категория существуют в базе данных.";
                }
                else if (ex.Message.Contains("timeout"))
                {
                    userMessage += "Превышено время ожидания ответа от сервера.\n" +
                                   "Проверьте подключение к сети и доступность SQL Server.";
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("connection"))
                {
                    userMessage += "Ошибка сетевого подключения к базе данных.";
                }
                else
                {
                    userMessage += $"Детали: {ex.Message}";
                }

                // Показываем сообщение пользователю
                MessageBox.Show(
                    userMessage,
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Обновляем статус
                StatusMessage = $"Ошибка: {ex.Message}";

                // Диалог не закрываем, результат отрицательный
                DialogResult = false;
            }
            finally
            {
                // Снимаем флаг сохранения
                IsSaving = false;
            }
        }

        private bool CanSave(object parameter)
        {
            // Проверяем валидность данных
            bool isTimeValid = DateTime.TryParseExact(DueTime, "HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _);

            var validation = ModelHelper.ValidateTask(CurrentTask);

            return !string.IsNullOrWhiteSpace(CurrentTask.Title) &&
                   CurrentTask.DueDate > DateTime.MinValue &&
                   SelectedPriority != null &&
                   isTimeValid &&
                   validation.isValid &&
                   !IsSaving;
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
            CloseWindow();
        }

        private void CloseWindow()
        {
            // Находим и закрываем окно
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

        // Реализация IDataErrorInfo для валидации
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
                        else if (!DateTime.TryParseExact(DueTime, "HH:mm",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out _))
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
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

            public void Execute(object parameter) => _execute(parameter);
        }
    }
}