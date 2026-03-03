using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop.Data
{
    // Вспомогательный класс для работы с базой данных
    public static class DatabaseHelper
    {
        // Строка подключения из конфигурации
        public static string ConnectionString => AppConfiguration.GetConnectionString();

        // Создает и возвращает новое подключение к базе данных
        public static SqlConnection CreateConnection()
        {
            try
            {
                return new SqlConnection(ConnectionString);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Не удалось создать подключение к базе данных", ex);
            }
        }

        // Создает параметр для SQL-команды
        public static SqlParameter CreateParameter(string name, object value)
        {
            var parameter = new SqlParameter(name, value ?? DBNull.Value);

            // Автоматически определяем тип для null значений
            if (value == null)
            {
                if (name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.SqlDbType = System.Data.SqlDbType.Int;
                }
                else if (name.Contains("Date", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.SqlDbType = System.Data.SqlDbType.DateTime2;
                }
                else
                {
                    parameter.SqlDbType = System.Data.SqlDbType.NVarChar;
                }
            }

            return parameter;
        }

        // Безопасно получает значение из DataReader
        public static T GetSafeValue<T>(SqlDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                if (value == DBNull.Value || value == null)
                {
                    return default;
                }
                return (T)value;
            }
            catch
            {
                return default;
            }
        }

        // Безопасно получает строку из DataReader
        public static string GetSafeString(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<string>(reader, columnName) ?? string.Empty;
        }

        // Безопасно получает DateTime из DataReader
        public static DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<DateTime>(reader, columnName);
        }

        // Безопасно получает Nullable DateTime из DataReader
        public static DateTime? GetSafeNullableDateTime(SqlDataReader reader, string columnName)
        {
            var value = reader[columnName];
            if (value == DBNull.Value || value == null)
            {
                return null;
            }
            return (DateTime)value;
        }

        // Безопасно получает boolean из DataReader
        public static bool GetSafeBoolean(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<bool>(reader, columnName);
        }

        // Безопасно получает integer из DataReader
        public static int GetSafeInt(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<int>(reader, columnName);
        }

        // Тестирует подключение к базе данных
        public static async Task<string> TestConnectionAsync()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    await connection.OpenAsync();

                    // Получаем информацию о сервере
                    var serverVersion = connection.ServerVersion;
                    var database = connection.Database;
                    var dataSource = connection.DataSource;

                    // Проверяем доступность таблиц
                    using (var command = new SqlCommand(
                        "SELECT " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Tasks') as HasTasks, " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Priorities') as HasPriorities, " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Categories') as HasCategories",
                        connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var hasTasks = reader.GetInt32(0) > 0;
                                var hasPriorities = reader.GetInt32(1) > 0;
                                var hasCategories = reader.GetInt32(2) > 0;

                                await reader.CloseAsync();
                                await connection.CloseAsync();

                                var tablesStatus =
                                    $"Таблицы: Tasks ({(hasTasks ? "+" : "-")}), " +
                                    $"Priorities ({(hasPriorities ? "+" : "-")}), " +
                                    $"Categories ({(hasCategories ? "+" : "-")})";

                                return $"Подключение успешно!\n" +
                                       $"Сервер: {dataSource}\n" +
                                       $"Версия: {serverVersion}\n" +
                                       $"База данных: {database}\n" +
                                       $"{tablesStatus}";
                            }
                        }
                    }
                }

                return "Подключение успешно, но не удалось проверить таблицы";
            }
            catch (SqlException sqlEx)
            {
                return $"Ошибка SQL Server: {sqlEx.Message}\n" +
                       $"Код ошибки: {sqlEx.Number}\n\n" +
                       $"Возможные решения:\n" +
                       $"1. Убедитесь, что SQL Server запущен\n" +
                       $"2. Проверьте строку подключения в appsettings.json\n" +
                       $"3. Убедитесь, что база данных 'TaskManagerDB' существует";
            }
            catch (Exception ex)
            {
                return $"Ошибка подключения: {ex.Message}";
            }
        }
    }
}