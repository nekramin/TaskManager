using System.Windows;

namespace TaskManagerDesktop
{
    public partial class TaskEditWindow : Window
    {
        public TaskEditViewModel ViewModel => (TaskEditViewModel)DataContext;

        public TaskEditWindow()
        {
            InitializeComponent();
            DataContext = new TaskEditViewModel(); // Или null, если нужно установить позже
            Owner = Application.Current.MainWindow;
            Loaded += (s, e) => TitleTextBox.Focus();
            InitializeKeyboardShortcuts();
        }

        public TaskEditWindow(TaskItem task = null) : this()
        {
            if (task != null)
            {
                DataContext = new TaskEditViewModel(task);
            }
        }

        // Выносим обработку клавиш в отдельный метод
        private void InitializeKeyboardShortcuts()
        {
            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter && ViewModel.SaveCommand.CanExecute(null))
                {
                    ViewModel.SaveCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    ViewModel.CancelCommand.Execute(null);
                    e.Handled = true;
                }
            };
        }

        // Свойство для получения созданной/отредактированной задачи
        public TaskItem EditedTask => ViewModel.DialogResult ? ViewModel.CurrentTask : null;
    }
}
