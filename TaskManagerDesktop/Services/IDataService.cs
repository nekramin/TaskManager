using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaskManagerDesktop.Services
{
    public interface IDataService
    {
        // Загрузка задач
        Task<List<TaskItem>> LoadTasksAsync();
        List<TaskItem> LoadTasks();

        // Проверка доступности БД
        Task<bool> IsDatabaseAvailableAsync();

        // Тестовые данные
        List<TaskItem> LoadSampleData();

        // Сохранение и обновление
        Task<TaskItem> SaveTaskAsync(TaskItem task);
        Task<bool> UpdateTaskAsync(TaskItem task);

        Task<bool> DeleteTaskAsync(int taskId);
    }
}