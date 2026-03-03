using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagerDesktop.Data;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop.Data.Repositories
{
    // Репозиторий для работы с категориями
    // Ссылок: 3
    public class CategoryRepository : IDisposable
    {
        private readonly DatabaseConnection _dbConnection;
        private bool _disposed = false;

        // Ссылок: 2
        public CategoryRepository()
        {
            _dbConnection = new DatabaseConnection();
        }

        // Получает все категории из базы данных
        // Ссылок: 1
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var categories = new List<Category>();

            string query = @"
                SELECT Id, Name, Description, IconCode, ColorCode
                FROM Categories
                ORDER BY Name ASC;
            ";

            SqlDataReader reader = null;
            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    var category = new Category
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeString(reader, "Description"),
                        IconCode = DatabaseHelper.GetSafeString(reader, "IconCode"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };

                    categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки категорий: {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }

            return categories;
        }

        // Получает категорию по ID
        // Ссылок: 1
        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            string query = @"
                SELECT Id, Name, Description, IconCode, ColorCode
                FROM Categories
                WHERE Id = @Id;
            ";

            var parameter = new SqlParameter("@Id", id);
            SqlDataReader reader = null;

            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query, parameter);

                if (await reader.ReadAsync())
                {
                    return new Category
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeString(reader, "Description"),
                        IconCode = DatabaseHelper.GetSafeString(reader, "IconCode"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };
                }
                else
                {
                    // Если категория не найдена, возвращаем null
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки категории (Id={id}): {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }
        }

        // Получает категорию по имени
        // Ссылок: 1
        public async Task<Category> GetCategoryByNameAsync(string name)
        {
            string query = @"
                SELECT Id, Name, Description, IconCode, ColorCode
                FROM Categories
                WHERE Name = @Name;
            ";

            var parameter = new SqlParameter("@Name", name);
            SqlDataReader reader = null;

            try
            {
                reader = await _dbConnection.ExecuteReaderAsync(query, parameter);

                if (await reader.ReadAsync())
                {
                    return new Category
                    {
                        Id = DatabaseHelper.GetSafeInt(reader, "Id"),
                        Name = DatabaseHelper.GetSafeString(reader, "Name"),
                        Description = DatabaseHelper.GetSafeString(reader, "Description"),
                        IconCode = DatabaseHelper.GetSafeString(reader, "IconCode"),
                        ColorCode = DatabaseHelper.GetSafeString(reader, "ColorCode")
                    };
                }
                else
                {
                    // Если не найдена, пытаемся найти "Работа"
                    return await GetCategoryByNameAsync("Работа") ?? new Category();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка загрузки категории (Name={name}): {ex.Message}", ex);
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();
            }
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
    }
}