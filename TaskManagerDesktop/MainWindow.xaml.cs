using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using TaskManagerDesktop.Services;


namespace TaskManagerDesktop
{
    public partial class MainWindow : Window
    {
        // ViewModel главного окна
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        // Таймер для обновления текущего времени в статусной строке
        private DispatcherTimer _timer;


        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем ViewModel как DataContext окна
            DataContext = new MainViewModel();

            // Подписываемся на события загрузки и закрытия окна
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            // Устанавливаем обработчик события PreviewKeyDown для всего окна
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализация таймера для обновления времени
            InitializeTimer();

            // Обновляем дату/время при загрузке
            UpdateDateTime();

            // Устанавливаем обработчик двойного клика по DataGrid
            TasksDataGrid.MouseDoubleClick += TasksDataGrid_MouseDoubleClick;

            // Устанавливаем начальный фокус на поле поиска для удобства пользователя
            SearchTextBox.Focus();

            // Обновляем статистику в строке состояния
            UpdateStatusBarStatistics();
        }


        // Обработчик события закрытия окна
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Останавливаем таймер при закрытии окна для освобождения ресурсов
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }


        // Инициализирует и запускает таймер для обновления времени
        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1); // обновление каждую секунду
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        // Обработчик события таймера
        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();

            // Проверяем просроченные задачи каждую минуту
            if (DateTime.Now.Second == 0)
            {
                ViewModel?.CheckForStatusChanges();
            }
        }


        // Обновляет отображение текущей даты и времени в строке состояния
        private void UpdateDateTime()
        {
            //Обновляем время в строке состояния
            if (CurrentDateText != null)
            {
                CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }
        }

        // Обновляет статистику в строке состояния
        private void UpdateStatusBarStatistics()
        {

        }


        // Обработчик двойного клика по DataGrid.
        private void TasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, что клик был именно по строке с задачей
            var source = e.OriginalSource as FrameworkElement;
            if (source?.DataContext is TaskItem task)
            {
                // Выбираем задачу
                ViewModel.SelectedTask = task;

                // Выполняем команду редактирования, если она доступна
                if (ViewModel.EditTaskCommand.CanExecute(task))
                {
                    ViewModel.EditTaskCommand.Execute(task);
                }

                // Предотвращаем дальнейшую обработку события
                e.Handled = true;
            }
        }

        // Обработчик изменения выбора в DataGrid.
        private void TasksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }


        // Обработчик глобальных горячих клавиш для всего окна.
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Обработка горячих клавиш для быстрого доступа к функциям

            // Ctrl+N – новая задача
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel.AddTaskCommand.CanExecute(null))
                {
                    ViewModel.AddTaskCommand.Execute(null);
                    e.Handled = true;
                }
            }

            // Delete - удалить задачу
            else if (e.Key == Key.Delete)
            {
                if (ViewModel.DeleteTaskCommand.CanExecute(null))
                {
                    ViewModel.DeleteTaskCommand.Execute(null);
                    e.Handled = true;
                }
            }

            // F5 – обновить список
            else if (e.Key == Key.F5)
            {
                if (ViewModel.RefreshCommand.CanExecute(null))
                {
                    ViewModel.RefreshCommand.Execute(null);
                    e.Handled = true;
                }
            }

            // Ctrl+F – фокус на поиск
            else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
            }

            // Escape – сброс поиска или закрытие диалогов
            else if (e.Key == Key.Escape)
            {
                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    SearchTextBox.Text = string.Empty;
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
            }
        }


        // Обработчик нажатия кнопки "Сбросить фильтры".
        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ClearFiltersCommand.CanExecute(null))
            {
                ViewModel.ClearFiltersCommand.Execute(null);
            }
        }


        // Обработчик нажатия CheckBox в DataGrid для отметки выполнения задачи
        private void CompletionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что событие вызвано CheckBox и у него есть DataContext
            if (sender is CheckBox checkBox && checkBox.DataContext is TaskItem task)
            {
                // Предотвращаем автоматическое изменение CheckBox
                checkBox.IsChecked = !task.IsCompleted; // Возвращаем предыдущее состояние

                // Выполняем команду отметки как выполненной
                if (ViewModel.MarkAsCompletedCommand.CanExecute(task))
                {
                    ViewModel.MarkAsCompletedCommand.Execute(task);
                }

                // Предотвращаем дальнейшую обработку события
                e.Handled = true;
            }
        }


        // Обработчик изменения текста в поле поиска.
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        // Обработчик изменения выбора в комбокоссе фильтров.
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // Обработчик изменения выбора в комбокоссе сортировки.
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // Обработчик изменения состояния CheckBox группировки.
        private void GroupingCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }


        // Показывает информацию о программе.
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "TaskManager Desktop v1.0\n\n" +
                "Приложение для управления задачами.\n" +
                "Разработано в рамках учебного проекта.\n\n" +
                "Используемые технологии:\n" +
                "- WPF (C#) для десктопной версии\n" +
                "- MVVM паттерн\n" +
                "- Команды и привязки данных\n" +
                "- Современный UI с эмодзи иконками",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        // Обработчик пункта меню "Экспорт задач".
        private void ExportTasksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Функция экспорта будет реализована в следующих версиях.",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        // Обработчик пункта меню "Импорт задач".
        private void ImportTasksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Функция импорта будет реализована в следующих версиях.",
                "В разработке",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }


        // Исходный метод TestDatabaseButton_Click
        private async void TestDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                button.Content = "Тестирование...";
            }

            try
            {
                // Создаем экземпляр DataService для тестирования
                var dataService = new DataService();

                // Проверяем доступность БД
                var isAvailable = await dataService.IsDatabaseAvailableAsync();

                if (isAvailable)
                {
                    // Загружаем тестовые данные из БД
                    var tasks = await dataService.LoadTasksAsync();

                    MessageBox.Show(
                        $"Подключение к БД успешно!\n\n" +
                        $"Загружено задач: {tasks.Count}\n" +
                        $"Примеры задач:\n" +
                        $"1. {tasks.FirstOrDefault()?.Title ?? "Нет задач"}\n" +
                        $"2. {tasks.Skip(1).FirstOrDefault()?.Title ?? ""}\n" +
                        $"3. {tasks.Skip(2).FirstOrDefault()?.Title ?? ""}",
                        "Тест БД - УСПЕХ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Не удалось подключиться к базе данных.\n\n" +
                        "Приложение будет использовать тестовые данные.",
                        "Тест БД - ОШИБКА",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при тестировании БД:\n{ex.Message}",
                    "Тест БД - ИСКЛЮЧЕНИЕ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Тест БД";
                }
            }
        }

        // Ссылок: 1
        private async void TestCrudButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Загружаем приоритеты и категории из БД
                var priorities = await ReferenceDataCache.GetPrioritiesAsync();
                var categories = await ReferenceDataCache.GetCategoriesAsync();

                if (priorities.Count == 0 || categories.Count == 0)
                {
                    MessageBox.Show("Не удалось загрузить справочные данные из БД.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Тест 1: Добавление новой задачи...", "Тест CRUD", MessageBoxButton.OK, MessageBoxImage.Information);

                var newTask = new TaskItem
                {
                    Title = $"Тестовая задача CRUD от {DateTime.Now:HH:mm:ss}",
                    Description = "Создана в процессе тестирования методов Add и Update.",
                    DueDate = DateTime.Now.AddDays(7),
                    Priority = priorities.FirstOrDefault(p => p.Name == "Medium") ?? priorities.First(),
                    Category = categories.FirstOrDefault(c => c.Name == "Работа") ?? categories.First(),
                    IsCompleted = false
                };

                var dataService = new DataService();
                var savedTask = await dataService.SaveTaskAsync(newTask);

                MessageBox.Show($"Задача добавлена успешно!\nID: {savedTask.Id}\nПриоритет ID: {savedTask.Priority.Id}\nКатегория ID: {savedTask.Category?.Id}\n\nОбновите список (F5).",
                    "Тест CRUD - УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);

                // Тест обновления
                if (savedTask.Id > 0)
                {
                    MessageBox.Show("Тест 2: Обновление задачи...", "Тест CRUD", MessageBoxButton.OK, MessageBoxImage.Information);
                    savedTask.Title = "ОБНОВЛЕННАЯ: " + savedTask.Title;
                    savedTask.IsCompleted = true;

                    bool updateResult = await dataService.UpdateTaskAsync(savedTask);
                    if (updateResult)
                    {
                        MessageBox.Show("Задача успешно обновлена в БД!\n\nОбновите список (F5).",
                            "Тест CRUD - УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования CRUD:\n{ex.Message}\n\n{ex.InnerException?.Message}",
                    "Тест CRUD - ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}