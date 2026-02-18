using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop.Data
{
    public static class DatabaseHelper
    {
        public static string ConnectionString => AppConfiguration.GetConnectionString();

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

        public static SqlParameter CreateParameter(string name, object value)
        {
            var parameter = new SqlParameter(name, value ?? DBNull.Value);

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

        public static string GetSafeString(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<string>(reader, columnName) ?? string.Empty;
        }

        public static DateTime GetSafeDateTime(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<DateTime>(reader, columnName);
        }

        public static DateTime? GetSafeNullableDateTime(SqlDataReader reader, string columnName)
        {
            var value = reader[columnName];
            if (value == DBNull.Value || value == null)
            {
                return null;
            }
            return (DateTime)value;
        }

        public static bool GetSafeBoolean(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<bool>(reader, columnName);
        }

        public static int GetSafeInt(SqlDataReader reader, string columnName)
        {
            return GetSafeValue<int>(reader, columnName);
        }

        public static async Task<string> TestConnectionAsync()
        {
            try
            {
                using (var connection = CreateConnection())
                {
                    await connection.OpenAsync();

                    var serverVersion = connection.ServerVersion;
                    var database = connection.Database;
                    var dataSource = connection.DataSource;

                    using (var command = new SqlCommand(
                        "SELECT " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Tasks') as HasTasks, " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Priorities') as HasPriorities, " +
                        "(SELECT COUNT(*) FROM sys.tables WHERE name = 'Categories') as HasCategories",
                        connection))
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
                    return "Подключение успешно, но не удалось проверить таблицы.";
                }
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