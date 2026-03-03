using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Data.Repositories;

namespace TaskManagerDesktop.Services
{
    // Сервис для работы с данными задач
    public class DataService : IDataService
    {
        private readonly INavigationService _navigationService;

        public DataService(INavigationService navigationService = null)
        {
            _navigationService = navigationService ?? NavigationService.Instance;
        }

        // Асинхронно загружает все задачи из базы данных
        public async Task<List<TaskItem>> LoadTasksAsync()
        {
            try
            {
                // Используем using для гарантированного освобождения ресурсов
                using (var repository = new TaskRepository())
                {
                    return await repository.GetAllTasksAsync();
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку для отладки
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки задач из БД: {ex}");

                // Показываем пользователю сообщение об ошибке
                await ShowErrorMessageAsync(
                    "Не удалось загрузить задачи из базы данных",
                    $"Ошибка: {ex.Message}\n\n" +
                    "Приложение будет использовать тестовые данные.");

                // Возвращаем тестовые данные в случае ошибки
                return LoadSampleData();
            }
        }

        // Синхронная версия загрузки задач
        public List<TaskItem> LoadTasks()
        {
            try
            {
                // Используем асинхронный метод синхронно (осторожно!)
                return LoadTasksAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки задач (синхронно): {ex}");
                return LoadSampleData();
            }
        }

        // Проверяет доступность базы данных
        public async Task<bool> IsDatabaseAvailableAsync()
        {
            try
            {
                using (var repository = new TaskRepository())
                {
                    // Пробуем выполнить простой запрос
                    var tasks = await repository.GetAllTasksAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Загружает тестовые данные
        public List<TaskItem> LoadSampleData()
        {
            var sampleTasks = ModelHelper.CreateSampleTasks();
            return new List<TaskItem>(sampleTasks);
        }

        // Показывает сообщение об ошибке (асинхронно)
        private async Task ShowErrorMessageAsync(string title, string message)
        {
            // Используем диспетчер для показа сообщения в UI-потоке
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }

        // Асинхронно сохраняет новую задачу в базу данных.
        // Ссылок: 2
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
                // Логирование и уведомление пользователя об ошибке
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения задачи: {ex}");
                await ShowErrorMessageAsync(
                    "Не удалось сохранить задачу",
                    $"Ошибка: {ex.Message}\n\nПроверьте корректность введенных данных и подключение к базе данных.");
                throw; // Пробрасываем исключение дальше, чтобы ViewModel мог обработать ошибку.
            }
        }

        // Ссылок: 1
        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            // 1. Проверка входного аргумента
            if (taskId <= 0)
            {
                throw new ArgumentException("ID задачи должен быть больше 0.", nameof(taskId));
            }

            try
            {
                // 2. Используем using для гарантированного освобождения ресурсов
                using (var repository = new TaskRepository())
                {
                    // 3. Вызываем метод репозитория
                    bool result = await repository.DeleteTaskAsync(taskId);

                    // 4. Логируем результат (в отладочном режиме)
                    if (result)
                    {
                        System.Diagnostics.Debug.WriteLine($"Задача с Id={taskId} успешно удалена из БД.");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Задача с Id={taskId} не найдена в БД.");
                    }

                    return result;
                }
            }
            catch (DatabaseException)
            {
                // Пробрасываем исключение дальше, чтобы ViewModel могла его обработать
                throw;
            }
            catch (Exception ex)
            {
                // Логируем неожиданную ошибку
                System.Diagnostics.Debug.WriteLine($"НЕОЖИДАННАЯ ОШИБКА ПРИ УДАЛЕНИИ ЗАДАЧИ Id={taskId}: {ex}");

                // Показываем пользователю сообщение об ошибке
                await ShowErrorMessageAsync(
                    "Не удалось удалить задачу",
                    $"При удалении задачи произошла ошибка:\n\n{ex.Message}\n\n" +
                    $"Пожалуйста, проверьте подключение к базе данных и повторите попытку.");

                throw;
            }
        }

        // Асинхронно обновляет существующую задачу в базе данных.
        // Ссылок: 2
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
                await ShowErrorMessageAsync(
                    "Не удалось обновить задачу",
                    $"Ошибка: {ex.Message}\n\nПроверьте корректность введенных данных.");
                throw;
            }
        }
    }
}
