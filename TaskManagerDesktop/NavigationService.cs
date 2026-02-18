using System;
using System.Windows;

namespace TaskManagerDesktop
{
    public interface INavigationService
    {
        bool ShowDialog<T>(object dataContext = null) where T : Window, new();
        void ShowWindow<T>(object dataContext = null) where T : Window, new();
        void CloseWindow(Window window);
    }

    public class NavigationService : INavigationService
    {
        private static NavigationService _instance;
        public static NavigationService Instance => _instance ??= new NavigationService();

        public bool ShowDialog<T>(object dataContext = null) where T : Window, new()
        {
            var window = new T();
            if (dataContext != null)
            {
                window.DataContext = dataContext;
            }

            if (Application.Current.MainWindow != null && Application.Current.MainWindow != window)
            {
                window.Owner = Application.Current.MainWindow;
            }

            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return window.ShowDialog() ?? false;
        }

        public void ShowWindow<T>(object dataContext = null) where T : Window, new()
        {
            var window = new T();
            if (dataContext != null)
            {
                window.DataContext = dataContext;
            }

            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        public void CloseWindow(Window window)
        {
            window.Close();
        }
    }
}