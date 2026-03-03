using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManagerDesktop
{
    /// <summary>
    /// Вспомогательный класс для работы с моделями данных
    /// </summary>
    public static class ModelHelper
    {
        /// <summary>
        /// Создает тестовую коллекцию задач для демонстрации
        /// </summary>
        public static ObservableCollection<TaskItem> CreateSampleTasks()
        {
            var tasks = new ObservableCollection<TaskItem>();
            var priorities = Priority.GetDefaultPriorities();
            var categories = Category.GetDefaultCategories();

            Random rnd = new Random();

            // Генерируем 10 тестовых задач
            for (int i = 1; i <= 10; i++)
            {
                var task = new TaskItem
                {
                    Id = i,
                    Title = $"Тестовая задача #{i}",
                    Description = $"Это тестовое описание для задачи #{i}. " +
                        $"Задача создана для демонстрации работы приложения.",
                    DueDate = DateTime.Now.AddDays(rnd.Next(-2, 7)),
                    IsCompleted = rnd.Next(0, 3) == 0, // 33% задач выполнены
                    Priority = priorities[rnd.Next(0, priorities.Count)],
                    Category = categories[rnd.Next(0, categories.Count)]
                };

                tasks.Add(task);
            }

            return tasks;
        }

        /// <summary>
        /// Преобразует строку в объект Priority
        /// </summary>
        public static Priority GetPriorityByName(string priorityName)
        {
            var priorities = Priority.GetDefaultPriorities();
            return priorities.FirstOrDefault(p =>
                p.Name.Equals(priorityName, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayName.Equals(priorityName, StringComparison.OrdinalIgnoreCase))
                ?? priorities.First(p => p.Name == "Medium");
        }

        /// <summary>
        /// Преобразует строку в объект Category
        /// </summary>
        public static Category GetCategoryByName(string categoryName)
        {
            var categories = Category.GetDefaultCategories();
            return categories.FirstOrDefault(c =>
                c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
                ?? categories.First(c => c.Name == "Работа");
        }

        /// <summary>
        /// Конвертирует старые строковые приоритеты в объекты Priority
        /// </summary>
        public static void ConvertStringPrioritiesToObjects(ObservableCollection<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                if (task.Priority == null || string.IsNullOrEmpty(task.Priority.Name))
                {
                    // Если задача имеет старый строковый формат приоритета
                    var priorityName = task.PriorityName;
                    task.Priority = GetPriorityByName(priorityName);
                }
            }
        }

        /// <summary>
        /// Конвертирует старые строковые категории в объекты Category
        /// </summary>
        public static void ConvertStringCategoriesToObjects(ObservableCollection<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                if (task.Category == null || string.IsNullOrEmpty(task.Category.Name))
                {
                    // Если задача имеет старый строковый формат категории
                    var categoryName = task.CategoryName;
                    task.Category = GetCategoryByName(categoryName);
                }
            }
        }

        /// <summary>
        /// Проверяет валидность задачи
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateTask(TaskItem task)
        {
            if (task == null)
                return (false, "Задача не может быть null");

            if (string.IsNullOrWhiteSpace(task.Title))
                return (false, "Название задачи обязательно");

            if (task.Title.Length > 200)
                return (false, "Название слишком длинное (максимум 200 символов)");

            if (task.Description?.Length > 1000)
                return (false, "Описание слишком длинное (максимум 1000 символов)");

            if (task.DueDate < DateTime.Now.AddMinutes(-5))
                return (false, "Дата выполнения не может быть в прошлом");

            if (task.Priority == null)
                return (false, "Приоритет не указан");

            if (task.Category == null)
                return (false, "Категория не указана");

            return (true, "Задача валидна");
        }
    }
}