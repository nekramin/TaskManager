using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Services;
using static TaskManagerDesktop.TaskEditViewModel;

namespace TaskManagerDesktop
{
    // ViewModel для главного окна приложения.
    public class MainViewModel : INotifyPropertyChanged
    {
        // Приватные поля для свойств
        private TaskItem _selectedTask;
        private string _statusMessage;
        private string _searchText;
        private string _selectedFilter = "Все задачи";
        private string _selectedSort = "По дате";
        private bool _isGroupByPriority;
        private bool _isLoading;

        // Зависимости
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private readonly CollectionViewSource _tasksViewSource;

        // Событие изменения свойств для уведомления UI.
        public event PropertyChangedEventHandler PropertyChanged;

        // Основная коллекция задач.
        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();

        // Представление задач с поддержкой фильтрации, сортировки и группировки.
        public ICollectionView TasksView => _tasksViewSource?.View;

        // Выбранная в DataGrid задача.
        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (_selectedTask != value)
                {
                    _selectedTask = value;
                    OnPropertyChanged(nameof(SelectedTask));
                    OnPropertyChanged(nameof(HasSelectedTask));

                    // Обновляем доступность команд, зависящих от выбранной задачи
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Указывает, есть ли выбранная задача.
        public bool HasSelectedTask => SelectedTask != null;

        // Текущее сообщение в строке состояния.
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

        // Текст для поиска задач.
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));

                    // Автоматическое обновление фильтрации при изменении текста поиска
                    ApplyFilter();
                }
            }
        }

        // Выбранный фильтр для отображения задач.
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value;
                    OnPropertyChanged(nameof(SelectedFilter));

                    // Применяем фильтр при изменении выбора
                    ApplyFilter();
                }
            }
        }

        // Выбранный способ сортировки задач.
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    OnPropertyChanged(nameof(SelectedSort));

                    // Применяем сортировку при изменении выбора
                    ApplySort();
                }
            }
        }

        // Флаг группировки задач по приоритету.
        public bool IsGroupByPriority
        {
            get => _isGroupByPriority;
            set
            {
                if (_isGroupByPriority != value)
                {
                    _isGroupByPriority = value;
                    OnPropertyChanged(nameof(IsGroupByPriority));

                    // Применяем группировку при изменении
                    ApplyGrouping();
                }
            }
        }

        // Флаг загрузки данных
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        // Статистические свойства (read-only)

        /// Общее количество задач.
        public int TotalTasks => Tasks.Count;

        /// Количество выполненных задач.
        public int CompletedTasks => Tasks.Count(t => t.IsCompleted);

        /// Количество активных (не выполненных) задач.
        public int ActiveTasks => Tasks.Count(t => !t.IsCompleted);

        /// Количество просроченных задач.
        public int OverdueTasks => Tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now);

        /// Количество срочных задач (дедлайн в течение суток).
        public int UrgentTasks => Tasks.Count(t => !t.IsCompleted &&
            t.DueDate >= DateTime.Now &&
            t.DueDate <= DateTime.Now.AddDays(1));

        // Команды

        // Команда добавления новой задачи.
        public ICommand AddTaskCommand { get; }

        // Команда редактирования выбранной задачи.
        public ICommand EditTaskCommand { get; }

        // Команда удаления выбранной задачи.
        public ICommand DeleteTaskCommand { get; }

        // Команда отметки задачи как выполненной.
        public ICommand MarkAsCompletedCommand { get; }

        // Команда обновления списка задач.
        public ICommand RefreshCommand { get; }

        // Команда сброса всех фильтров.
        public ICommand ClearFiltersCommand { get; }

        // Команда выхода из приложения.
        public ICommand ExitCommand { get; }

        // Команда для обработки двойного клика по задаче.
        public ICommand DoubleClickCommand { get; }

        // Команда экспорта задач.
        public ICommand ExportCommand { get; }

        public ICommand ApplyFilterCommand { get; }

        // Команда загрузки данных
        public ICommand LoadDataCommand { get; }

        // Конструктор ViewModel.
        public MainViewModel()
        {
            // Инициализация сервиса навигации
            _navigationService = NavigationService.Instance;

            _dataService = new DataService(_navigationService);

            // Инициализация CollectionViewSource для фильтрации и сортировки
            _tasksViewSource = new CollectionViewSource();
            _tasksViewSource.Source = Tasks;

            // Инициализация команд
            AddTaskCommand = new RelayCommand(AddTask);
            EditTaskCommand = new RelayCommand(EditTask, CanEditTask);
            DeleteTaskCommand = new AsyncRelayCommand(async () => await DeleteTaskAsync());
            MarkAsCompletedCommand = new RelayCommand(MarkAsCompleted, CanMarkAsCompleted);
            RefreshCommand = new AsyncRelayCommand(async () => await RefreshAsync());
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExitCommand = new RelayCommand(Exit);
            DoubleClickCommand = new RelayCommand(OnDoubleClick, CanEditTask);
            ExportCommand = new RelayCommand(ExportTasks);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            LoadDataCommand = new AsyncRelayCommand(async () => await LoadDataAsync());

            // Начальные значения
            StatusMessage = "Готов к работе. Для начала добавьте задачу или используйте тестовые данные.";

            // Загрузка данных (асинхронно, чтобы не блокировать UI)
            _ = LoadDataAsync();

            // Настройка представления (фильтрация, сортировка, группировка)
            ConfigureView();
        }

        // Настраивает представление задач (фильтрация, сортировка, группировка).
        private void ConfigureView()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            // Начальная группировка по статусу (Выполненные/Активные)
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));

            // Начальная сортировка по дате (сначала срочные)
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));

            // Начальный фильтр
            view.Filter = FilterTask;
        }

        // Загружает тестовые данные для демонстрации работы приложения.
        private void LoadSampleData()
        {
            // Очищаем существующие задачи
            Tasks.Clear();

            // Используем ModelHelper для создания тестовых данных
            var sampleTasks = ModelHelper.CreateSampleTasks();

            foreach (var task in sampleTasks)
            {
                Tasks.Add(task);
            }

            // Обновляем статистику после загрузки данных
            UpdateStatistics();

            // Обновляем статусное сообщение
            StatusMessage = $"Загружено {Tasks.Count} тестовых задач. Готово к работе.";
        }

        // Добавление новой задачи
        // Ссылок: 1
        private void AddTask(object parameter)
        {
            try
            {
                // Создаем ViewModel для новой задачи
                var taskViewModel = new TaskEditViewModel(null, _navigationService);

                // Показываем диалоговое окно через навигационный сервис
                var result = _navigationService.ShowDialog<TaskEditWindow>(taskViewModel);

                // Если пользователь нажал "Сохранить"
                if (result == true && taskViewModel.CurrentTask != null)
                {
                    // Теперь CurrentTask содержит актуальные данные из БД (с Id)
                    // Добавляем её в коллекцию
                    Tasks.Add(taskViewModel.CurrentTask);

                    // Выбираем новую задачу
                    SelectedTask = taskViewModel.CurrentTask;

                    // Обновляем статусное сообщение
                    StatusMessage = $"Добавлена новая задача: '{taskViewModel.CurrentTask.Title}' (ID: {taskViewModel.CurrentTask.Id})";

                    // Обновляем фильтрацию и статистику
                    ApplyFilter();
                    UpdateStatistics();
                }
                else if (result == false)
                {
                    StatusMessage = "Добавление задачи отменено";
                }
            }
            catch (Exception ex)
            {
                // Обработка ошибок с пользовательским сообщением
                StatusMessage = $"Ошибка при добавлении задачи: {ex.Message}";
                MessageBox.Show($"Не удалось добавить задачу:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Логирование для отладки
                Debug.WriteLine($"Ошибка в AddTask: {ex}");
            }
        }

        // Асинхронно загружает данные из базы данных
        public async Task LoadDataAsync()
        {
            // Проверяем, не идет ли уже загрузка
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Загрузка задач из базы данных...";

                // Очищаем существующие задачи
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Tasks.Clear();
                });

                // Загружаем задачи из базы данных через сервис
                var tasks = await _dataService.LoadTasksAsync();

                // Добавляем задачи в коллекцию (в UI-потоке)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var task in tasks)
                    {
                        Tasks.Add(task);
                    }
                });

                // Обновляем статистику после загрузки данных
                UpdateStatistics();

                // Выбираем первую задачу, если есть
                if (Tasks.Count > 0)
                {
                    SelectedTask = Tasks.First();
                }

                // Обновляем статусное сообщение
                StatusMessage = $"Загружено {Tasks.Count} задач из базы данных. Готово к работе.";

                // Обновляем представление
                ApplyFilter();
            }
            catch (Exception ex)
            {
                // В случае ошибки показываем сообщение и используем тестовые данные
                StatusMessage = $"Ошибка загрузки данных: {ex.Message}";

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Загружаем тестовые данные
                    LoadSampleData();
                    StatusMessage = $"Используются тестовые данные. Загружено {Tasks.Count} задач.";
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Метод для асинхронного обновления списка задач
        private async Task RefreshAsync()
        {
            StatusMessage = "Обновление списка задач...";
            await LoadDataAsync();
        }

        // Проверка возможности редактирования задачи.
        private bool CanEditTask(object parameter) => HasSelectedTask;

        // Редактирование выбранной задачи.
        private void EditTask(object parameter)
        {
            if (SelectedTask == null) return;

            try
            {
                // Создаем ViewModel для редактирования выбранной задачи
                var taskViewModel = new TaskEditViewModel(SelectedTask, _navigationService);

                // Показываем диалоговое окно через навигационный сервис
                var result = _navigationService.ShowDialog<TaskEditWindow>(taskViewModel);

                // Если пользователь сохранил изменения
                if (result == true)
                {
                    // Обновляем статусное сообщение
                    StatusMessage = $"Задача '{SelectedTask.Title}' обновлена";

                    // Обновляем представление и статистику
                    ApplyFilter();
                    UpdateStatistics();
                }
                else if (result == false)
                {
                    StatusMessage = "Редактирование задачи отменено";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при редактировании задачи: {ex.Message}";
                MessageBox.Show($"Не удалось отредактировать задачу:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Ошибка в EditTask: {ex}");
            }
        }

        // Проверка возможности удаления задачи.
        private bool CanDeleteTask(object parameter) => HasSelectedTask;

        // Удаление выбранной задачи.
        // Ссылок: 1
        private async Task DeleteTaskAsync()
        {
            // 1. Проверяем, что задача выбрана
            if (SelectedTask == null)
            {
                StatusMessage = "Не выбрана задача для удаления.";
                return;
            }

            // 2. Сохраняем ссылку на удаляемую задачу
            var taskToDelete = SelectedTask;
            var taskId = taskToDelete.Id;
            var taskTitle = taskToDelete.Title;

            // 3. Запрашиваем подтверждение у пользователя
            var confirmResult = MessageBox.Show(
                $"Вы уверены, что хотите удалить задачу '{taskTitle}'?\n\n" +
                $"ID: {taskId}\n" +
                $"Дата: {taskToDelete.DueDate:dd.MM.yyyy HH:mm}\n" +
                $"Приоритет: {taskToDelete.Priority?.DisplayName ?? taskToDelete.PriorityName}",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (confirmResult != MessageBoxResult.Yes)
            {
                StatusMessage = "Удаление задачи отменено пользователем.";
                return;
            }

            // 4. Пытаемся удалить задачу из базы данных
            try
            {
                // Показываем индикатор загрузки
                IsLoading = true;
                StatusMessage = $"Удаление задачи '{taskTitle}' из базы данных...";

                // Вызываем сервис данных для удаления
                bool deleteResult = await _dataService.DeleteTaskAsync(taskId);

                if (deleteResult)
                {
                    // 5. Если успешно удалено из БД - удаляем из коллекции
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Удаляем задачу из коллекции
                        Tasks.Remove(taskToDelete);

                        // Сбрасываем выбранную задачу
                        SelectedTask = null;

                        // Обновляем статусное сообщение
                        StatusMessage = $"Задача '{taskTitle}' (ID: {taskId}) успешно удалена.";

                        // Обновляем статистику
                        UpdateStatistics();

                        // Обновляем фильтрацию
                        ApplyFilter();
                    });
                }
                else
                {
                    // 6. Если задача не найдена в БД (уже удалена?)
                    StatusMessage = $"Задача '{taskTitle}' (ID: {taskId}) не найдена в базе данных.";

                    // Удаляем из коллекции, т.к. в БД её уже нет
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Tasks.Remove(taskToDelete);
                        SelectedTask = null;
                        UpdateStatistics();
                        ApplyFilter();
                    });

                    MessageBox.Show(
                        $"Задача не найдена в базе данных.\n\n" +
                        $"Возможно, она была удалена другим пользователем или приложением.\n" +
                        $"Список задач будет обновлен.",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (DatabaseException dbEx)
            {
                // 7. Обработка специфических ошибок БД
                StatusMessage = $"Ошибка базы данных при удалении задачи.";

                MessageBox.Show(
                    $"Не удалось удалить задачу из базы данных.\n\n" +
                    $"Тип ошибки: {dbEx.GetType().Name}\n" +
                    $"Сообщение: {dbEx.Message}\n\n" +
                    $"Проверьте:\n" +
                    $"1. Подключение к SQL Server\n" +
                    $"2. Права на удаление записей\n" +
                    $"3. Наличие таблицы Tasks в базе данных",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"DatabaseException в DeleteTask: {dbEx}");
            }
            catch (Exception ex)
            {
                // 8. Обработка неожиданных ошибок
                StatusMessage = $"Неожиданная ошибка при удалении задачи.";

                MessageBox.Show(
                    $"Произошла неожиданная ошибка при удалении задачи:\n\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                System.Diagnostics.Debug.WriteLine($"Exception в DeleteTask: {ex}");
            }
            finally
            {
                // 9. Снимаем индикатор загрузки
                IsLoading = false;
            }
        }

        // Отметка выбранной задачи как выполненной.
        private bool CanMarkAsCompleted(object parameter) =>
            HasSelectedTask && !SelectedTask.IsCompleted;

        // Отметка выбранной задачи как выполненной.
        private void MarkAsCompleted(object parameter)
        {
            if (SelectedTask == null) return;

            // Запрашиваем подтверждение
            var result = MessageBox.Show(
                $"Отметить задачу '{SelectedTask.Title}' как выполненную?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Изменяем статус задачи
                SelectedTask.IsCompleted = true;

                // Обновляем статусное сообщение
                StatusMessage = $"Задача '{SelectedTask.Title}' отмечена как выполненная";

                // Обновляем представление и статистику
                ApplyFilter();
                UpdateStatistics();

                // Обновляем доступность команд
                CommandManager.InvalidateRequerySuggested();
            }
            else
            {
                StatusMessage = "Действие отменено";
            }
        }

        // Обновление списка задач.
        private void Refresh(object parameter)
        {
            StatusMessage = "Обновление списка задач...";

            ApplyFilter();

            StatusMessage = "Список задач обновлен";
        }

        // Сброс всех фильтров и сортировки.
        private void ClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedFilter = "Все задачи";
            SelectedSort = "По дате";
            IsGroupByPriority = false;

            // Применяем сброшенные настройки
            ApplyFilter();
            ApplySort();
            ApplyGrouping();

            StatusMessage = "Фильтры сброшены";
        }

        // Выход из приложения.
        private void Exit(object parameter)
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите выйти из приложения?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
            else
            {
                StatusMessage = "Выход отменен";
            }
        }

        // Обработка двойного клика по задаче.
        private void OnDoubleClick(object parameter)
        {
            EditTask(parameter);
        }

        // Экспорт задач.
        private void ExportTasks(object parameter)
        {
            MessageBox.Show(
                "Функция экспорта будет реализована в следующих версиях.\n" +
                "Планируется экспорт в форматы: CSV, Excel, PDF.",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            StatusMessage = "Экспорт задач (в разработке)";
        }

        // Проверка изменения статусов задач.
        public void CheckForStatusChanges()
        {
            bool hasChanges = false;

            foreach (var task in Tasks)
            {
                // Если задача не выполнена и просрочена
                if (!task.IsCompleted && task.DueDate < DateTime.Now)
                {
                    hasChanges = true;
                    break;
                }
            }

            // Если есть изменения, обновляем представление
            if (hasChanges)
            {
                ApplyFilter();
                UpdateStatistics();
            }
        }


        // Методы для фильтрации, сортировки и группировки

        // Применяет текущий фильтр к представлению задач.
        private void ApplyFilter(object parameter = null)
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            // Сбрасываем фильтр перед установкой нового
            view.Filter = null;
            view.Filter = FilterTask;

            // Обновляем статистику и сообщение
            UpdateStatistics();
            UpdateStatusMessage();
        }

        // Функция фильтрации для CollectionView.
        // Ссылок: 3
        private bool FilterTask(object obj)
        {
            if (obj is not TaskItem task) return false;

            // 1. Фильтр по тексту поиска
            if (!string.IsNullOrEmpty(SearchText))
            {
                string searchLower = SearchText.ToLower();
                bool matchesSearch = task.Title.ToLower().Contains(searchLower) ||
                    (task.Description?.ToLower()?.Contains(searchLower) ?? false);
                if (!matchesSearch) return false;
            }

            // 2. Фильтр по статусу и срочности
            switch (SelectedFilter)
            {
                case "Активные":
                    if (task.IsCompleted) return false;
                    break;

                case "Выполненные":
                    if (!task.IsCompleted) return false;
                    break;

                case "Просроченные":
                    // Просроченные = не выполнены И дата выполнения меньше текущей
                    if (task.IsCompleted || task.DueDate >= DateTime.Now)
                        return false;
                    break;

                case "Срочные":
                    // Срочные = не выполнены И дата выполнения в течение 24 часов
                    if (task.IsCompleted || task.DueDate > DateTime.Now.AddDays(1))
                        return false;
                    break;

                // Фильтры по категориям
                case "Работа":
                    if (task.Category?.Name != "Работа") return false;
                    break;

                case "Личное":
                    if (task.Category?.Name != "Личное") return false;
                    break;

                case "Учеба":
                    if (task.Category?.Name != "Учеба") return false;
                    break;

                case "Здоровье":
                    if (task.Category?.Name != "Здоровье") return false;
                    break;

                case "Финансы":
                    if (task.Category?.Name != "Финансы") return false;
                    break;

                case "Все задачи":
                default:
                    // Все задачи проходят фильтр
                    break;
            }

            return true;
        }

        // Применяет текущую сортировку к представлению задач.
        private void ApplySort()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            view.SortDescriptions.Clear();

            switch (SelectedSort)
            {
                case "По дате":
                    view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));
                    break;

                case "По приоритету":
                    view.SortDescriptions.Add(new SortDescription("PriorityOrder", ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending)); // Вторичная сортировка
                    break;

                case "По названию":
                    view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
                    break;

                case "По категории":
                    view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));
                    break;
            }
        }

        // Применяет текущую группировку к представлению задач.
        private void ApplyGrouping()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            view.GroupDescriptions.Clear();

            if (IsGroupByPriority)
            {
                // Группировка по приоритету
                view.GroupDescriptions.Add(new PropertyGroupDescription("Priority"));
            }
            else
            {
                // Группировка по статусу (Выполненные/Активные)
                view.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));
            }

            UpdateStatusMessage();
        }

        // Обновляет статусное сообщение с информацией о текущем представлении.
        // Ссылок: 3
        private void UpdateStatusMessage()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            // Подсчет отфильтрованных задач
            int filteredCount = 0;
            foreach (var item in view.SourceCollection)
            {
                if (FilterTask(item)) filteredCount++;
            }

            string groupingText = IsGroupByPriority ? " (сгруппировано по приоритету)" : "";

            // ДОБАВЛЯЕМ описание для "Просроченные"
            string filterDescription = SelectedFilter switch
            {
                "Активные" => "показаны только активные задачи",
                "Выполненные" => "показаны только выполненные задачи",
                "Просроченные" => "показаны только просроченные задачи",
                "Срочные" => "показаны только срочные задачи (24 часа)",
                "Все задачи" => "показаны все задачи",
                _ => $"фильтр по категории: {SelectedFilter}"
            };

            StatusMessage = $"Показано {filteredCount} из {TotalTasks} задач{groupingText}. " +
                $"{filterDescription} | Сортировка: {SelectedSort}";
        }

        // Обновляет статистические свойства и уведомляет UI об изменениях.
        private void UpdateStatistics()
        {
            // Уведомляем UI об изменении всех статистических свойств
            OnPropertyChanged(nameof(TotalTasks));
            OnPropertyChanged(nameof(CompletedTasks));
            OnPropertyChanged(nameof(ActiveTasks));
            OnPropertyChanged(nameof(OverdueTasks));
            OnPropertyChanged(nameof(UrgentTasks));
        }

        // Вызывает событие PropertyChanged для уведомления UI об изменении свойства.
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}