using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using TaskManagerDesktop.Data.Repositories;

namespace TaskManagerDesktop.Services
{
    public class DataService : IDataService
    {
        private readonly INavigationService _navigationService;

        public DataService(INavigationService navigationService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
        }

        public async Task<List<TaskItem>> LoadTasksAsync()
        {
            try
            {
                using (var repository = new TaskRepository())
                {
                    return await repository.GetAllTasksAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки задач из БД: {ex}");

                await ShowErrorMessageAsync(
                    "Не удалось загрузить задачи из базы данных",
                    $"Ошибка: {ex.Message}\n\n" +
                    "Приложение будет использовать тестовые данные."
                );

                return LoadSampleData();
            }
        }

        public List<TaskItem> LoadTasks()
        {
            try
            {
                return LoadTasksAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки задач (синхронно): {ex}");
                return LoadSampleData();
            }
        }

        public async Task<bool> IsDatabaseAvailableAsync()
        {
            try
            {
                using (var repository = new TaskRepository())
                {
                    var tasks = await repository.GetAllTasksAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public List<TaskItem> LoadSampleData()
        {
            var sampleTasks = ModelHelper.CreateSampleTasks();
            return new List<TaskItem>(sampleTasks);
        }

        private async Task ShowErrorMessageAsync(string title, string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        public async Task<TaskItem> SaveTaskAsync(TaskItem task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            try
            {
                using (var repository = new TaskRepository())
                {
                    return await repository.AddTaskAsync(task);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения задачи: {ex}");
                await ShowErrorMessageAsync("Не удалось сохранить задачу",
                                           $"Ошибка: {ex.Message}\n\n" +
                                           "Проверьте корректность введённых данных и подключение к базе данных.");
                throw;
            }
        }

        public async Task<bool> UpdateTaskAsync(TaskItem task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            try
            {
                using (var repository = new TaskRepository())
                {
                    int rowsAffected = await repository.UpdateTaskAsync(task);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка обновления задачи (Id={task.Id}): {ex}");
                await ShowErrorMessageAsync("Не удалось обновить задачу",
                                           $"Ошибка: {ex.Message}\n\n" +
                                           "Проверьте корректность введённых данных.");
                throw;
            }
        }
    }
}