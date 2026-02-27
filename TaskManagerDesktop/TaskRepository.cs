using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Infrastructure;
using static TaskManagerDesktop.Infrastructure.AppConfiguration;

namespace TaskManagerDesktop.Data.Repositories
{
    public class TaskRepository : IDisposable
    {
        private readonly DatabaseConnection _dbConnection;
        private bool _disposed = false;

        public TaskRepository()
        {
            _dbConnection = new DatabaseConnection();
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            var tasks = new List<TaskItem>();
            string query = @"
                SELECT 
                    t.Id AS TaskId, t.Title AS TaskTitle, t.Description AS TaskDescription, 
                    t.DueDate AS TaskDueDate, t.IsCompleted AS TaskIsCompleted, 
                    t.PriorityId, t.CategoryId,
                    p.Id AS PriorityId, p.Name AS PriorityName, p.DisplayName AS PriorityDisplayName, 
                    p.Level AS PriorityLevel, p.ColorCode AS PriorityColorCode,
                    c.Id AS CategoryId, c.Name AS CategoryName, c.Description AS CategoryDescription, 
                    c.IconCode AS CategoryIconCode, c.ColorCode AS CategoryColorCode
                FROM Tasks t
                INNER JOIN Priorities p ON t.PriorityId = p.Id
                INNER JOIN Categories c ON t.CategoryId = c.Id
                ORDER BY t.DueDate ASC;";

            SqlDataReader reader = null;
            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query);
                while (await reader.ReadAsync())
                {
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

                    var task = new TaskItem
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "TaskId"),
                        Title = DatabaseHelper.GetSafeString(reader, "TaskTitle"),
                        Description = DatabaseHelper.GetSafeString(reader, "TaskDescription"),
                        DueDate = DatabaseHelper.GetSafeDateTime(reader, "TaskDueDate"),
                        IsCompleted = DatabaseHelper.GetSafeBoolean(reader, "TaskIsCompleted"),
                        Priority = priority,
                        Category = category
                    };

                    tasks.Add(task);
                }
            }
            catch (SqlException sqlEx)
            {
                throw new DatabaseException("Ошибка при загрузке задач из базы данных.", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Неожиданная ошибка при загрузке задач.", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }

            return tasks;
        }

        public List<TaskItem> GetAllTasks()
        {
            return GetAllTasksAsync().GetAwaiter().GetResult();
        }

        public async Task<TaskItem> AddTaskAsync(TaskItem task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task), "Объект задачи не может быть null.");

            string query = @"
                INSERT INTO Tasks (Title, Description, DueDate, PriorityId, CategoryId, IsCompleted)
                VALUES (@Title, @Description, @DueDate, @PriorityId, @CategoryId, @IsCompleted);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                ";

            var parameters = new[]
            {
            new SqlParameter("@Title", task.Title),
            new SqlParameter("@Description", (object)task.Description ?? DBNull.Value),
            new SqlParameter("@DueDate", task.DueDate),
            new SqlParameter("@PriorityId", task.PriorityId),
            new SqlParameter("@CategoryId", (object)task.CategoryId ?? DBNull.Value),
            new SqlParameter("@IsCompleted", task.IsCompleted),
            };

            SqlDataReader reader = null;
            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query, parameters);

                if (await reader.ReadAsync())
                {
                    var priorityId = DatabaseHelper.GetSafeInt(reader, "PriorityId");
                    var categoryValue = reader["CategoryId"];
                    int? categoryId = categoryIdValue == DBNull.Value ? null : (int?)Convert.ToInt32(categoryIdValue);

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
                throw new DatabaseException($"Ошибка SQL Server при добавлении задачи '{task.Title}'. Проверьте корректность данных (например, существование PriorityId = {task.PriorityId}). Детали: {sqlEx.Message}", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Неожиданная ошибка при добавлении задачи: {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }
        }

        public async Task<TaskItem> UpdateTaskAsync(TaskItem task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task), "Объект задачи не может быть null.");

            if (task.Id <= 0)
                throw new ArgumentException("Id задачи должен быть больше 0 для операции обновления.", nameof(task.Id));

            string query = @"
            UPDATE Tasks 
            SET 
            Title = @Title,
            Description = @Description,
            DueDate = @DueDate,
            PriorityId = @PriorityId,
            CategoryId = @CategoryId,
            IsCompleted = @IsCompleted
            WHERE Id = @Id;";

            var parameters = new[]
            {
            new SqlParameter("@Id", task.Id),
            new SqlParameter("@Title", task.Title),
            new SqlParameter("@Description", task.Description ?? DBNull.Value),
            new SqlParameter("@DueDate", task.DueDate),
            new SqlParameter("@PriorityId", task.PriorityId),
            new SqlParameter("@CategoryId", task.CategoryId ?? DBNull.Value),
            new SqlParameter("@IsCompleted", task.IsCompleted)
            };

            try
            {
                int rowsAffected = await _dbConnection.ExecuteNonQueryAsync(query, parameters);

                if (rowsAffected == 0)
                {
                    throw new DatabaseException($"Не удалось обновить задачу с Id={task.Id}. Запись не найдена.");
                }

                return rowsAffected;
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
        public TaskItem AddTask(TaskItem task)
        {
            return AddTaskAsync(task).GetAwaiter().GetResult();
        }

        public int UpdateTask(TaskItem task)
        {
            return UpdateTaskAsync(task).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbConnection?.Dispose();
                }
                _disposed = true;
            }
        }

        ~TaskRepository()
        {
            Dispose(false);
        }
    }
}