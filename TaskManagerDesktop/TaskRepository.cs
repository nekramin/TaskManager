using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Infrastructure;

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