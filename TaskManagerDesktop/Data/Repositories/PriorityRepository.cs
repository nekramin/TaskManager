using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop.Data.Repositories
{
    // Репозиторий для работы с приоритетами
    // Ссылок: 3
    public class PriorityRepository : IDisposable
    {
        private readonly DatabaseConnection _dbConnection;
        private bool _disposed = false;

        // Ссылок: 2
        public PriorityRepository()
        {
            _dbConnection = new DatabaseConnection();
        }

        // Получает все приоритеты из базы данных
        // Ссылок: 1
        public async Task<List<Priority>> GetAllPrioritiesAsync()
        {
            var priorities = new List<Priority>();

            string query = @"
                SELECT Id, Name, DisplayName, Level, ColorCode
                FROM Priorities
                ORDER BY Level ASC;
            ";

            SqlDataReader reader = null;
            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    var priority = new Priority
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        DisplayName = DatabaseHelper.GetSafeString(reader, "DisplayName"),
                        Level = DatabaseHelper.GetSafeInt(reader, "Level"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };

                    priorities.Add(priority);
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки приоритетов: {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }

            return priorities;
        }

        // Получает приоритет по ID
        // Ссылок: 1
        public async Task<Priority> GetPriorityByIdAsync(int id)
        {
            string query = @"
                SELECT Id, Name, DisplayName, Level, ColorCode
                FROM Priorities
                WHERE Id = @Id;
            ";

            var parameter = new SqlParameter("@Id", id);
            SqlDataReader reader = null;

            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query, parameter);

                if (await reader.ReadAsync())
                {
                    return new Priority
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        DisplayName = DatabaseHelper.GetSafeString(reader, "DisplayName"),
                        Level = DatabaseHelper.GetSafeInt(reader, "Level"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };
                }
                else
                {
                    // Если приоритет не найден, возвращаем "Средний" как fallback
                    return new Priority
                    {
                        Id = 1004, // ID Medium из БД
                        Name = "Medium",
                        DisplayName = "Средний",
                        Level = 2,
                        ColorCode = "#FF9800"
                    };
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки приоритета (Id={id}): {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }
        }

        // Получает приоритет по имени
        // Ссылок: 1
        public async Task<Priority> GetPriorityByNameAsync(string name)
        {
            string query = @"
                SELECT Id, Name, DisplayName, Level, ColorCode
                FROM Priorities
                WHERE Name = @Name;
            ";

            var parameter = new SqlParameter("@Name", name);
            SqlDataReader reader = null;

            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query, parameter);

                if (await reader.ReadAsync())
                {
                    return new Priority
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        DisplayName = DatabaseHelper.GetSafeString(reader, "DisplayName"),
                        Level = DatabaseHelper.GetSafeInt(reader, "Level"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };
                }
                else
                {
                    // Если не найден, пытаемся найти "Medium"
                    return await GetPriorityByNameAsync("Medium") ?? new Priority();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки приоритета (Name={name}): {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
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
    }
}
