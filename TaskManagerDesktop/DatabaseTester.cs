using System;
using System.Threading.Tasks;
using System.Windows;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Data.Repositories;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop
{
    // Класс для тестирования подключения к базе данных
    public static class DatabaseTester
    {
        // Тестирует всю цепочку подключения к БД
        public static async Task TestDatabaseConnectionAsync()
        {
            try
            {
                // Тест 1: Проверка конфигурации
                var configResult = AppConfiguration.TestConfiguration();
                MessageBox.Show(configResult, "Тест конфигурации",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Тест 2: Проверка подключения через DatabaseHelper
                var connectionTest = await DatabaseHelper.TestConnectionAsync();
                MessageBox.Show(connectionTest, "Тест подключения",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Тест 3: Проверка работы DatabaseConnection
                using (var db = new DatabaseConnection())
                {
                    var isConnected = await db.TestConnectionAsync();
                    if (isConnected)
                    {
                        // Пример выполнения запроса
                        var query = "SELECT COUNT(*) FROM Priorities";
                        var count = await db.ExecuteScalarAsync(query);

                        MessageBox.Show($"DatabaseConnection работает!\n" +
                            $"Количество приоритетов в базе: {count}",
                            "Тест DatabaseConnection",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования базы данных:\n{ex.Message}\n\n" +
                    $"Тип ошибки: {ex.GetType().Name}\n" +
                    $"Подробности: {ex.InnerException?.Message}",
                    "Критическая ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task TestTaskRepositoryAsync()
        {
            string resultMessage = "Тестирование репозитория задач...\n";

            // Используем using для гарантированного освобождения ресурсов репозитория.
            using (var repository = new TaskRepository())
            {
                try
                {
                    resultMessage += "1. Создание репозитория... УСПЕХ\n";

                    // 1. Пробуем загрузить задачи.
                    resultMessage += "2. Загрузка задач из БД... ";
                    var tasks = await repository.GetAllTasksAsync();

                    resultMessage += $"УСПЕХ. Загружено задач: {tasks.Count}\n";

                    // 2. Выводим информацию о первых 3 задачах (или всех, если меньше).
                    resultMessage += "3. Примеры загруженных задач:\n";
                    int displayCount = Math.Min(tasks.Count, 3);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var task = tasks[i];
                        resultMessage += $"\n   Задача #{i + 1}:\n";
                        resultMessage += $"   ID: {task.Id}\n";
                        resultMessage += $"   Название: {task.Title}\n";
                        resultMessage += $"   Приоритет: {task.Priority?.DisplayName} (Уровень: {task.Priority?.Level})\n";
                        resultMessage += $"   Категория: {task.Category?.Name} ({task.Category?.IconCode})\n";
                        resultMessage += $"   Срок: {task.DueDate:dd.MM.yyyy HH:mm}\n";
                        resultMessage += $"   Статус: {(task.IsCompleted ? "Выполнена" : "Активна")}\n";
                    }

                    if (tasks.Count == 0)
                    {
                        resultMessage += "\n   В базе данных нет задач. Добавьте задачи через SQL Management Studio.\n";
                    }

                    resultMessage += $"\nИтог: Репозиторий задач работает корректно. Всего задач в БД: {tasks.Count}";

                    MessageBox.Show(resultMessage, "Тест репозитория - УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    // Более детальный вывод ошибки.
                    resultMessage += $"ОШИБКА!\n\n";
                    resultMessage += $"Тип исключения: {ex.GetType().Name}\n";
                    resultMessage += $"Сообщение: {ex.Message}\n";

                    // Если есть внутреннее исключение, выводим и его.
                    if (ex.InnerException != null)
                    {
                        resultMessage += $"\nВнутреннее исключение:\n";
                        resultMessage += $"Тип: {ex.InnerException.GetType().Name}\n";
                        resultMessage += $"Сообщение: {ex.InnerException.Message}\n";
                    }

                    resultMessage += $"\nРекомендации:\n";
                    resultMessage += $"1. Убедитесь, что база данных 'TaskManagerDB' существует и запущена.\n";
                    resultMessage += $"2. Проверьте, созданы ли таблицы Tasks, Priorities, Categories.\n";
                    resultMessage += $"3. Проверьте, заполнены ли таблицы Priorities и Categories (должны быть данные из GetDefaultPriorities/GetDefaultCategories).\n";
                    resultMessage += $"4. Убедитесь, что в таблице Tasks есть корректные PriorityId и CategoryId, ссылающиеся на существующие записи.\n";

                    MessageBox.Show(resultMessage, "Тест репозитория - ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}