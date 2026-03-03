using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Infrastructure;
using TaskManagerDesktop.Services;

namespace TaskManagerDesktop.Data.Repositories
{
    // Репозиторий для работы с задачами (Tasks) в базе данных.
    public class TaskRepository : IDisposable
    {
        // Приватное поле для хранения подключения к БД.
        private readonly DatabaseConnection _dbConnection;
        private bool _disposed = false;

        // Конструктор репозитория. Инициализирует новое подключение к базе данных.
        public TaskRepository()
        {
            // Создаем экземпляр DatabaseConnection, который управляет жизненным циклом SqlConnection.
            _dbConnection = new DatabaseConnection();
        }

        // Асинхронно получает все задачи из таблицы Tasks.
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            // Список, в который будем складывать результаты.
            var tasks = new List<TaskItem>();

            // SQL-запрос.
            // Соединяем (JOIN) таблицы Tasks, Priorities и Categories,
            // чтобы сразу получить все необходимые данные для создания объектов.
            string query = @"
                SELECT 
                    t.Id AS TaskId,
                    t.Title AS TaskTitle,
                    t.Description AS TaskDescription,
                    t.DueDate AS TaskDueDate,
                    t.IsCompleted AS TaskIsCompleted,
                    t.PriorityId,
                    t.CategoryId,
                    -- Данные из таблицы Priorities
                    p.Id AS PriorityId,
                    p.Name AS PriorityName,
                    p.DisplayName AS PriorityDisplayName,
                    p.Level AS PriorityLevel,
                    p.ColorCode AS PriorityColorCode,
                    -- Данные из таблицы Categories
                    c.Id AS CategoryId,
                    c.Name AS CategoryName,
                    c.Description AS CategoryDescription,
                    c.IconCode AS CategoryIconCode,
                    c.ColorCode AS CategoryColorCode
                FROM Tasks t
                INNER JOIN Priorities p ON t.PriorityId = p.Id
                INNER JOIN Categories c ON t.CategoryId = c.Id
                ORDER BY t.DueDate ASC; -- Сортируем по дате выполнения по возрастанию
            ";

            // SqlDataReader - объект для последовательного чтения данных из результата запроса.
            SqlDataReader reader = null;

            try
            {
                // Выполняем асинхронный запрос к базе данных и получаем reader.
                reader = await _dbConnection.ExecuteReaderAsync(query);

                // Читаем данные построчно, пока есть строки.
                while (await reader.ReadAsync())
                {
                    // Для каждой строки создаем объекты Priority и Category.
                    // Используем вспомогательные методы из DatabaseHelper для безопасного чтения.
                    var priority = new Priority
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "PriorityId"),
                        Name = DatabaseHelper.GetSafeString(reader, "PriorityName"),
                        DisplayName = DatabaseHelper.GetSafeString(reader, "PriorityDisplayName"),
                        Level = DatabaseHelper.GetSafeInt(reader, "PriorityLevel"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "PriorityColorCode")
                    };

                    var category = new Category
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "CategoryId"),
                        Name = DatabaseHelper.GetSafeString(reader, "CategoryName"),
                        Description = DatabaseHelper.GetSafeString(reader, "CategoryDescription"),
                        IconCode = DatabaseHelper.GetSafeString(reader, "CategoryIconCode"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "CategoryColorCode")
                    };

                    // Маппинг основных полей задачи.
                    var task = new TaskItem
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "TaskId"),
                        Title = DatabaseHelper.GetSafeString(reader, "TaskTitle"),
                        Description = DatabaseHelper.GetSafeString(reader, "TaskDescription"),
                        DueDate = DatabaseHelper.GetSafeDateTime(reader, "TaskDueDate"),
                        IsCompleted = DatabaseHelper.GetSafeBoolean(reader, "TaskIsCompleted"),
                        // Присваиваем созданные объекты.
                        Priority = priority,
                        Category = category
                    };

                    // Добавляем задачу в итоговый список.
                    tasks.Add(task);
                }
            }
            catch (SqlException sqlEx)
            {
                // Обрабатываем специфические ошибки SQL Server.
                throw new DatabaseException($"Ошибка при загрузке задач из базы данных. Проверьте SQL-запрос и подключение.\nДетали: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                // Обрабатываем любые другие исключения.
                throw new DatabaseException($"Неожиданная ошибка при загрузке задач: {ex.Message}", ex);
            }
            finally
            {
                // Всегда закрываем reader, если он был открыт.
                reader?.Close();
                reader?.Dispose();
            }

            // Возвращаем заполненный список задач.
            return tasks;
        }

        // Синхронная версия метода GetAllTasks.
        public List<TaskItem> GetAllTasks()
        {
            // Вызываем асинхронный метод и синхронно ожидаем его завершения.
            return GetAllTasksAsync().GetAwaiter().GetResult();
        }

        public async Task<TaskItem> AddTaskAsync(TaskItem task)
        {
            // 1. Проверка входного аргумента
            if (task == null)
                throw new ArgumentNullException(nameof(task), "Объект задачи не может быть null.");

            // 2. SQL-запрос для вставки новой записи.
            string query = @"
            INSERT INTO Tasks (Title, Description, DueDate, PriorityId, CategoryId, IsCompleted)
            OUTPUT INSERTED.*
            VALUES (@Title, @Description, @DueDate, @PriorityId, @CategoryId, @IsCompleted);
            ";

            // 3. Подготовка параметров SQL-запроса.
            var parameters = new[]
            {
            new SqlParameter("@Title", task.Title),
            new SqlParameter("@Description", (object)task.Description ?? DBNull.Value), // NULL, если описание пустое
            new SqlParameter("@DueDate", task.DueDate),
            new SqlParameter("@PriorityId", task.PriorityId),
            // CategoryId может быть NULL в БД. Передаем DBNull.Value, если в объекте категория не установлена.
            new SqlParameter("@CategoryId", (object)task.CategoryId ?? DBNull.Value),
            new SqlParameter("@IsCompleted", task.IsCompleted)
            };

            SqlDataReader reader = null;
            try
            {
                // 4. Выполнение запроса.
                reader = await _dbConnection.ExecuteReaderAsync(query, parameters);

                // 5. Чтение результата.
                if (await reader.ReadAsync())
                {
                    // Получаем ID из БД
                    var priorityId = DatabaseHelper.GetSafeInt(reader, "PriorityId");
                    var categoryIdValue = reader["CategoryId"];
                    int? categoryId = categoryIdValue == DBNull.Value ? null : (int?)Convert.ToInt32(categoryIdValue);

                    // Загружаем полные объекты из кэша/БД
                    var priority = await ReferenceDataCache.GetPriorityByIdAsync(priorityId);
                    var category = categoryId.HasValue ?
                        await ReferenceDataCache.GetCategoryByIdAsync(categoryId) : null;

                    var insertedTask = new TaskItem
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Title = DatabaseHelper.GetSafeString(reader, "Title"),
                        Description = DatabaseHelper.GetSafeString(reader, "Description"),
                        DueDate = DatabaseHelper.GetSafeDateTime(reader, "DueDate"),
                        IsCompleted = DatabaseHelper.GetSafeBoolean(reader, "IsCompleted"),
                        Priority = priority,
                        Category = category
                    };

                    task.Id = insertedTask.Id;
                    return insertedTask;
                }
                else
                {
                    throw new DatabaseException("Не удалось добавить задачу. База данных не вернула данные новой записи.");
                }
            }
            catch (SqlException sqlEx)
            {
                // Специфичная обработка ошибок SQL Server
                throw new DatabaseException($"Ошибка SQL Server при добавлении задачи '{task.Title}'. Проверьте корректность данных (например, существование PriorityId={task.PriorityId}). Детали: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Неожиданная ошибка при добавлении задачи: {ex.Message}", ex);
            }
            finally
            {
                // 6. Освобождение ресурсов SqlDataReader.
                reader?.Close();
                reader?.Dispose();
            }
        }

        public async Task<int> UpdateTaskAsync(TaskItem task)
        {
            // 1. Проверка входного аргумента
            if (task == null)
                throw new ArgumentNullException(nameof(task), "Объект задачи не может быть null.");
            if (task.Id <= 0)
                throw new ArgumentException("Id задачи должен быть больше 0 для операции обновления.", nameof(task.Id));

            // 2. SQL-запрос для обновления записи по Id.
            //    Обновляем все поля, кроме Id и CreatedDate (они не должны меняться).
            string query = @"
            UPDATE Tasks
            SET
            Title = @Title,
            Description = @Description,
            DueDate = @DueDate,
            PriorityId = @PriorityId,
            CategoryId = @CategoryId,
            IsCompleted = @IsCompleted
            WHERE Id = @Id;
            ";

            // 3. Подготовка параметров SQL-запроса.
            var parameters = new[]
            {
            new SqlParameter("@Id", task.Id),
            new SqlParameter("@Title", task.Title),
            new SqlParameter("@Description", (object)task.Description ?? DBNull.Value),
            new SqlParameter("@DueDate", task.DueDate),
            new SqlParameter("@PriorityId", task.PriorityId),
            new SqlParameter("@CategoryId", (object)task.CategoryId ?? DBNull.Value),
            new SqlParameter("@IsCompleted", task.IsCompleted)
            };

            try
            {
                // 4. Выполнение команды UPDATE.
                // ExecuteNonQueryAsync возвращает количество обработанных строк.
                int rowsAffected = await _dbConnection.ExecuteNonQueryAsync(query, parameters);

                // 5. Проверка результата.
                if (rowsAffected == 0)
                {
                    // Если ни одна строка не обновилась, возможно, задачи с таким Id не существует.
                    throw new DatabaseException($"Не удалось обновить задачу с Id={task.Id}. Запись не найдена.");
                }

                return rowsAffected; // В норме должно вернуться 1.
            }
            catch (SqlException sqlEx)
            {
                throw new DatabaseException($"Ошибка SQL Server при обновлении задачи (Id={task.Id}, Title='{task.Title}'). Детали: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Неожиданная ошибка при обновлении задачи: {ex.Message}", ex);
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

            // 2. SQL-запрос для удаления записи по Id
            string query = @"
        DELETE FROM Tasks
        WHERE Id = @Id;
    ";

            // 3. Подготовка параметров SQL-запроса
            var parameter = new SqlParameter("@Id", taskId);

            try
            {
                // 4. Выполнение команды DELETE
                // ExecuteNonQueryAsync возвращает количество удаленных строк
                int rowsAffected = await _dbConnection.ExecuteNonQueryAsync(query, parameter);

                // 5. Возвращаем true, если удалена хотя бы одна строка
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx)
            {
                // Специфичная обработка ошибок SQL Server
                throw new DatabaseException(
                    $"Ошибка SQL Server при удалении задачи с Id={taskId}. " +
                    $"Детали: {sqlEx.Message}",
                    sqlEx);
            }
            catch (Exception ex)
            {
                // Обрабатываем любые другие исключения
                throw new DatabaseException(
                    $"Неожиданная ошибка при удалении задачи с Id={taskId}: {ex.Message}",
                    ex);
            }
        }



        // Синхронно удаляет задачу из базы данных по её идентификатору.
        // Ссылок: 0
        public bool DeleteTask(int taskId)
        {
            // Вызываем асинхронный метод и синхронно ожидаем его завершения
            return DeleteTaskAsync(taskId).GetAwaiter().GetResult();
        }

        // Синхронная версия метода AddTaskAsync.
        // Ссылок: 0
        public TaskItem AddTask(TaskItem task)
        {
            return AddTaskAsync(task).GetAwaiter().GetResult();
        }

        // Синхронная версия метода UpdateTaskAsync.
        // Ссылок: 0
        public int UpdateTask(TaskItem task)
        {
            return UpdateTaskAsync(task).GetAwaiter().GetResult();
        }



        // Реализация интерфейса IDisposable для корректного освобождения ресурсов.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Защищенный метод для освобождения управляемых ресурсов.
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы.
                    _dbConnection?.Dispose();
                }
                _disposed = true;
            }
        }

        // Финализатор.
        ~TaskRepository()
        {
            Dispose(false);
        }
    }
}