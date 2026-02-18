using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;

namespace TaskManagerDesktop
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeTimer();
            UpdateDateTime();
            TasksDataGrid.MouseDoubleClick += TasksDataGrid_MouseDoubleClick;
            SearchTextBox.Focus();
            UpdateStatusBarStatistics();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
            if (DateTime.Now.Second == 0)
            {
                ViewModel?.CheckForStatusChanges();
            }
        }

        private void UpdateDateTime()
        {
            if (CurrentDateText != null)
            {
                CurrentDateText.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            }
        }

        private void UpdateStatusBarStatistics()
        {
            if (ListStatsTextBlock != null)
            {
                ListStatsTextBlock.Text = $"Всего задач: {ViewModel.TotalTasks} Выполнено: {ViewModel.CompletedTasks}";
            }
        }

        private void TasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            if (source?.DataContext is TaskItem task)
            {
                ViewModel.SelectedTask = task;
                if (ViewModel.EditTaskCommand.CanExecute(task))
                {
                    ViewModel.EditTaskCommand.Execute(task);
                }
                e.Handled = true;
            }
        }

        private void TasksDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обработчик изменения выбора в DataGrid
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (ViewModel.AddTaskCommand.CanExecute(null))
                {
                    ViewModel.AddTaskCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Delete)
            {
                if (ViewModel.DeleteTaskCommand.CanExecute(null))
                {
                    ViewModel.DeleteTaskCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                if (ViewModel.RefreshCommand.CanExecute(null))
                {
                    ViewModel.RefreshCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    SearchTextBox.Text = string.Empty;
                    SearchTextBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.ClearFiltersCommand.CanExecute(null))
            {
                ViewModel.ClearFiltersCommand.Execute(null);
            }
        }

        private void CompletionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if (checkBox?.DataContext is TaskItem task)
            {
                checkBox.IsChecked = !task.IsCompleted;
                if (ViewModel.MarkAsCompletedCommand.CanExecute(task))
                {
                    ViewModel.MarkAsCompletedCommand.Execute(task);
                }
                e.Handled = true;
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GroupingCheckBox_Changed(object sender, RoutedEventArgs e)
        {

        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TaskManager Desktop v1.0\n\n" +
                            "Приложение для управления задачами.\n" +
                            "Разработано в рамках учебного проекта.\n\n" +
                            "Используемые технологии:\n" +
                            "- WPF (C#) для десктопной версии\n" +
                            "- MVVM паттерн\n" +
                            "- Команды и привязки данных\n" +
                            "- Современный UI с эмодзи и иконками",
                            "О программе", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportTasksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция экспорта будет реализована в следующих версиях.",
                            "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void TestDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                button.Content = "Тестируется...";
            }

            var result = MessageBox.Show(
                "Выберите тест:\nДа - тест подключения к БД\nНет - тест репозитория задач",
                "Выбор теста",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question
            );

            try
            {
                if (result == MessageBoxResult.Yes)
                {
                    await DatabaseTester.TestDatabaseConnectionAsync();
                }
                else if (result == MessageBoxResult.No)
                {
                    await DatabaseTester.TestTaskRepositoryAsync();
                }
            }
            finally
            {
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Тест БД (C#)";
                }
            }
        }
        private void ImportTasksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция импорта будет реализована в следующих версиях.",
                            "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}