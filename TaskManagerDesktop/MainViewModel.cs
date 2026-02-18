using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace TaskManagerDesktop
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private TaskItem _selectedTask;
        private string _statusMessage;
        private string _searchText;
        private string _selectedFilter = "Все задачи";
        private string _selectedSort = "По дате";
        private bool _isGroupByPriority;
        private readonly INavigationService _navigationService;
        private readonly CollectionViewSource _tasksViewSource;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TaskItem> Tasks { get; } = new ObservableCollection<TaskItem>();
        public ICollectionView TasksView => _tasksViewSource.View;

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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasSelectedTask => SelectedTask != null;

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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplyFilter();
                }
            }
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                if (_selectedFilter != value)
                {
                    _selectedFilter = value;
                    OnPropertyChanged(nameof(SelectedFilter));
                    ApplyFilter();
                }
            }
        }

        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    OnPropertyChanged(nameof(SelectedSort));
                    ApplySort();
                }
            }
        }

        public bool IsGroupByPriority
        {
            get => _isGroupByPriority;
            set
            {
                if (_isGroupByPriority != value)
                {
                    _isGroupByPriority = value;
                    OnPropertyChanged(nameof(IsGroupByPriority));
                    ApplyGrouping();
                }
            }
        }

        public int TotalTasks => Tasks.Count;
        public int CompletedTasks => Tasks.Count(t => t.IsCompleted);
        public int ActiveTasks => Tasks.Count(t => !t.IsCompleted);
        public int OverdueTasks => Tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now);
        public int UrgentTasks => Tasks.Count(t => !t.IsCompleted && t.DueDate >= DateTime.Now && t.DueDate <= DateTime.Now.AddDays(1));

        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand MarkAsCompletedCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand DoubleClickCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ApplyFilterCommand { get; }

        public MainViewModel()
        {
            _navigationService = NavigationService.Instance;
            _tasksViewSource = new CollectionViewSource { Source = Tasks };

            AddTaskCommand = new RelayCommand(AddTask);
            EditTaskCommand = new RelayCommand(EditTask, CanEditTask);
            DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
            MarkAsCompletedCommand = new RelayCommand(MarkAsCompleted, CanMarkAsCompleted);
            RefreshCommand = new RelayCommand(Refresh);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ExitCommand = new RelayCommand(Exit);
            DoubleClickCommand = new RelayCommand(OnDoubleClick, CanEditTask);
            ExportCommand = new RelayCommand(ExportTasks);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);

            StatusMessage = "Готово к работе. Для начала добавьте задачу.";
            LoadSampleData();
            ConfigureView();
        }

        private void ConfigureView()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));

            view.Filter = FilterTask;
        }

        private void LoadSampleData()
        {
            Tasks.Clear();
            var sampleTasks = ModelHelper.CreateSampleTasks();
            foreach (var task in sampleTasks)
            {
                Tasks.Add(task);
            }
            UpdateStatistics();
            UpdateStatusMessage();
        }

        private void AddTask(object parameter)
        {
            try
            {
                var taskViewModel = new TaskEditViewModel(null, _navigationService);
                var result = _navigationService.ShowDialog<TaskEditWindow>(taskViewModel);
                if (result == true && taskViewModel.CurrentTask != null)
                {
                    Tasks.Add(taskViewModel.CurrentTask);
                    SelectedTask = taskViewModel.CurrentTask;
                    StatusMessage = $"Добавлена новая задача: '{taskViewModel.CurrentTask.Title}'";
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
                StatusMessage = $"Ошибка при добавлении задачи: {ex.Message}";
                MessageBox.Show($"Не удалось добавить задачу:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Ошибка в AddTask: {ex}");
            }
        }

        private bool CanEditTask(object parameter) => HasSelectedTask;

        private void EditTask(object parameter)
        {
            if (SelectedTask == null) return;
            try
            {
                var taskViewModel = new TaskEditViewModel(SelectedTask, _navigationService);
                var result = _navigationService.ShowDialog<TaskEditWindow>(taskViewModel);
                if (result == true)
                {
                    StatusMessage = $"Задача '{SelectedTask.Title}' обновлена";
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
                MessageBox.Show($"Не удалось отредактировать задачу:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Ошибка в EditTask: {ex}");
            }
        }

        private bool CanDeleteTask(object parameter) => HasSelectedTask;

        private void DeleteTask(object parameter)
        {
            if (SelectedTask == null) return;
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить задачу '{SelectedTask.Title}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var taskToDelete = SelectedTask;
                Tasks.Remove(taskToDelete);
                SelectedTask = null;
                StatusMessage = $"Задача '{taskToDelete.Title}' удалена";
                UpdateStatistics();
            }
            else
            {
                StatusMessage = "Удаление задачи отменено";
            }
        }

        private bool CanMarkAsCompleted(object parameter) => HasSelectedTask && !SelectedTask.IsCompleted;

        private void MarkAsCompleted(object parameter)
        {
            if (SelectedTask == null) return;
            var result = MessageBox.Show(
                $"Отметить задачу '{SelectedTask.Title}' как выполненную?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SelectedTask.IsCompleted = true;
                StatusMessage = $"Задача '{SelectedTask.Title}' отмечена как выполненная";
                ApplyFilter();
                UpdateStatistics();
            }
            else
            {
                StatusMessage = "Действие отменено";
            }
        }

        private void Refresh(object parameter)
        {
            StatusMessage = "Обновление списка задач...";
            ApplyFilter();
            StatusMessage = "Список задач обновлён";
        }

        private void ClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedFilter = "Все задачи";
            SelectedSort = "По дате";
            IsGroupByPriority = false;
            ApplyFilter();
            ApplySort();
            ApplyGrouping();
            StatusMessage = "Фильтры сброшены";
        }

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
                StatusMessage = "Выход отменён";
            }
        }

        private void OnDoubleClick(object parameter)
        {
            EditTask(parameter);
        }

        private void ExportTasks(object parameter)
        {
            MessageBox.Show(
                "Функция экспорта будет реализована в следующих версиях.\n\nПланируется экспорт в форматы: CSV, Excel, PDF.",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            StatusMessage = "Экспорт задач (в разработке)";
        }

        private void ApplyFilter(object parameter = null)
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            view.Filter = null;
            view.Filter = FilterTask;

            UpdateStatistics();
            UpdateStatusMessage();
        }

        public void CheckForStatusChanges()
        {
            bool hasChanges = false;
            foreach (var task in Tasks)
            {
                if (!task.IsCompleted && task.DueDate < DateTime.Now)
                {
                    hasChanges = true;
                    break;
                }
            }

            if (hasChanges)
            {
                ApplyFilter();
                UpdateStatistics();
            }
        }

        private bool FilterTask(object obj)
        {
            if (!(obj is TaskItem task)) return false;

            if (!string.IsNullOrEmpty(SearchText))
            {
                string searchLower = SearchText.ToLower();
                bool matchesSearch =
                    task.Title.ToLower().Contains(searchLower) ||
                    (task.Description?.ToLower()?.Contains(searchLower) ?? false);

                if (!matchesSearch) return false;
            }

            switch (SelectedFilter)
            {
                case "Активные":
                    if (task.IsCompleted) return false;
                    break;
                case "Выполненные":
                    if (!task.IsCompleted) return false;
                    break;
                case "Просроченные":
                    if (task.IsCompleted || task.DueDate >= DateTime.Now) return false;
                    break;
                case "Срочные":
                    if (task.IsCompleted || task.DueDate > DateTime.Now.AddDays(1)) return false;
                    break;
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
                case "Семья":
                    if (task.Category?.Name != "Семья") return false;
                    break;
                case "Отдых":
                    if (task.Category?.Name != "Отдых") return false;
                    break;
                case "Все задачи":
                default:
                    break;
            }

            return true;
        }

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
                    view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));
                    break;
                case "По названию":
                    view.SortDescriptions.Add(new SortDescription("Title", ListSortDirection.Ascending));
                    break;
                case "По категории":
                    view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                    view.SortDescriptions.Add(new SortDescription("DueDate", ListSortDirection.Ascending));
                    break;
            }

            UpdateStatusMessage();
        }

        private void ApplyGrouping()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            view.GroupDescriptions.Clear();
            if (IsGroupByPriority)
            {
                view.GroupDescriptions.Add(new PropertyGroupDescription("Priority"));
            }
            else
            {
                view.GroupDescriptions.Add(new PropertyGroupDescription("GroupKey"));
            }

            UpdateStatusMessage();
        }

        private void UpdateStatusMessage()
        {
            var view = _tasksViewSource.View;
            if (view == null) return;

            int filteredCount = 0;
            foreach (var item in view.SourceCollection)
            {
                if (FilterTask(item)) filteredCount++;
            }

            string groupingText = IsGroupByPriority ? " (группировано по приоритету)" : "";
            StatusMessage = $"Показано {filteredCount} из {TotalTasks} задач{groupingText}.\nФильтр: {SelectedFilter}\nСортировка: {SelectedSort}";
        }

        private void UpdateStatistics()
        {
            OnPropertyChanged(nameof(TotalTasks));
            OnPropertyChanged(nameof(CompletedTasks));
            OnPropertyChanged(nameof(ActiveTasks));
            OnPropertyChanged(nameof(OverdueTasks));
            OnPropertyChanged(nameof(UrgentTasks));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
