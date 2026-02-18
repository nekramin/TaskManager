using System.Threading.Tasks;
using System.Windows;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Data.Repositories;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop
{
    public static class DatabaseTester
    {
        public static async Task TestDatabaseConnectionAsync()
        {
            try
            {
                var configResult = AppConfiguration.TestConfiguration();
                MessageBox.Show(configResult, "Тест конфигурации", MessageBoxButton.OK, MessageBoxImage.Information);

                var connectionTest = await DatabaseHelper.TestConnectionAsync();
                MessageBox.Show(connectionTest, "Тест подключения", MessageBoxButton.OK, MessageBoxImage.Information);

                using (var db = new DatabaseConnection())
                {
                    var isConnected = await db.TestConnectionAsync();
                    if (isConnected)
                    {
                        var query = "SELECT COUNT(*) FROM Priorities";
                        var count = await db.ExecuteScalarAsync(query);
                        MessageBox.Show($"DatabaseConnection работает!\nКоличество приоритетов в базе: {count}", "Тест DatabaseConnection", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка тестирования базы данных:\n{ex.Message}\nТип ошибки: {ex.GetType().Name}\nПодробности: {ex.InnerException?.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task TestTaskRepositoryAsync()
        {
            string resultMessage = "Тестирование репозитория задач...\n";
            using (var repository = new TaskRepository())
            {
                try
                {
                    resultMessage += "1. Создание репозитория... УСПЕХ\n";
                    resultMessage += "2. Загрузка задач из БД... ";
                    var tasks = await repository.GetAllTasksAsync();
                    resultMessage += $"УСПЕХ. Загружено задач: {tasks.Count}\n";

                    resultMessage += "3. Примеры загруженных задач:\n";
                    int displayCount = Math.Min(tasks.Count, 3);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var task = tasks[i];
                        resultMessage += $"\nЗадача #{i + 1}:\n";
                        resultMessage += $"ID: {task.Id}\n";
                        resultMessage += $"Название: {task.Title}\n";
                        resultMessage += $"Приоритет: {task.Priority?.DisplayName} (Уровень: {task.Priority?.Level})\n";
                        resultMessage += $"Категория: {task.Category?.Name} ({task.Category?.IconCode})\n";
                        resultMessage += $"Срок: {task.DueDate.ToString("dd.MM.yyyy HH:mm")}\n";
                        resultMessage += $"Статус: {(task.IsCompleted ? "Выполнена" : "Активна")}";
                    }

                    if (tasks.Count == 0)
                    {
                        resultMessage += "\nВ базе данных нет задач. Добавьте задачи через SQL Management Studio.";
                    }

                    resultMessage += $"\nИтог: Репозиторий задач работает корректно. Всего задач в БД: {tasks.Count}.";
                    MessageBox.Show(resultMessage, "Тест репозитория - УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    resultMessage += $"ОШИБКА!\n\n";
                    resultMessage += $"Тип исключения: {ex.GetType().Name}\n";
                    resultMessage += $"Сообщение: {ex.Message}\n";
                    if (ex.InnerException != null)
                    {
                        resultMessage += $"\nВнутреннее исключение:\n";
                        resultMessage += $"Тип: {ex.InnerException.GetType().Name}\n";
                        resultMessage += $"Сообщение: {ex.InnerException.Message}\n";
                    }

                    resultMessage += "\nРекомендации:\n";
                    resultMessage += "1. Убедитесь, что база данных \"TaskManagerDB\" существует и запущена.\n";
                    resultMessage += "2. Проверьте, созданы ли таблицы Tasks, Priorities, Categories.\n";
                    resultMessage += "3. Проверьте, заполнены ли таблицы Priorities и Categories (должны быть данные из GetDefaultPriorities/GetDefaultCategories).\n";
                    resultMessage += "4. Убедитесь, что в таблице Tasks есть корректные значения PriorityId и CategoryId, ссылающиеся на существующие записи.\n";
                    MessageBox.Show(resultMessage, "Тест репозитория - ОШИБКА", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}