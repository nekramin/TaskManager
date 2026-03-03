using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using TaskManagerDesktop.Infrastructure;

namespace TaskManagerDesktop.Data
{
    public class DatabaseConnection : IDisposable
    {
        private SqlConnection _connection;
        private bool _disposed = false;
        private readonly int _commandTimeout;

        // Создает новый экземпляр DatabaseConnection
        public DatabaseConnection()
        {
            // Получаем настройки из конфигурации
            _commandTimeout = AppConfiguration.GetDatabaseSetting("CommandTimeout", 30);
        }

        // Активное подключение к базе данных
        public SqlConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    InitializeConnection();
                }
                return _connection;
            }
        }

        // Таймаут команд в секундах
        public int CommandTimeout => _commandTimeout;

        // Инициализирует подключение к базе данных
        private void InitializeConnection()
        {
            try
            {
                // Получаем строку подключения из конфигурации
                var connectionString = AppConfiguration.GetConnectionString();

                // Создаем новое подключение
                _connection = new SqlConnection(connectionString);

                // Открываем подключение
                OpenConnection();

                // Логируем успешное подключение (в отладочном режиме)
                System.Diagnostics.Debug.WriteLine($"Подключение к БД установлено: {_connection.Database}");
            }
            catch (SqlException sqlEx)
            {
                // Обрабатываем ошибки SQL Server
                throw new DatabaseException("Ошибка подключения к базе данных", sqlEx);
            }
            catch (Exception ex)
            {
                // Обрабатываем другие ошибки
                throw new DatabaseException("Ошибка инициализации подключения", ex);
            }
        }

        // Открывает подключение к базе данных
        public void OpenConnection()
        {
            if (_connection == null)
            {
                InitializeConnection();
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        // Асинхронно открывает подключение к базе данных
        public async Task OpenConnectionAsync()
        {
            if (_connection == null)
            {
                InitializeConnection();
            }

            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        // Закрывает подключение к базе данных
        public void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        // Тестирует подключение к базе данных
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Создаем временное подключение для теста
                using (var testConnection = new SqlConnection(AppConfiguration.GetConnectionString()))
                {
                    await testConnection.OpenAsync();
                    await testConnection.CloseAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Выполняет SQL-запрос и возвращает SqlDataReader
        public SqlDataReader ExecuteReader(string query, params SqlParameter[] parameters)
        {
            try
            {
                OpenConnection();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка выполнения запроса: {query}", ex);
            }
        }

        // Асинхронно выполняет SQL-запрос и возвращает SqlDataReader
        public async Task<SqlDataReader> ExecuteReaderAsync(string query, params SqlParameter[] parameters)
        {
            try
            {
                await OpenConnectionAsync();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Асинхронная ошибка выполнения запроса: {query}", ex);
            }
        }

        // Выполняет SQL-команду (INSERT, UPDATE, DELETE)
        public int ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            try
            {
                OpenConnection();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка выполнения команды: {query}", ex);
            }
            finally
            {
                // Всегда закрываем подключение после выполнения команды
                CloseConnection();
            }
        }

        // Асинхронно выполняет SQL-команду
        public async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
        {
            try
            {
                await OpenConnectionAsync();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Асинхронная ошибка выполнения команды: {query}", ex);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Выполняет SQL-запрос и возвращает скалярное значение
        public object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            try
            {
                OpenConnection();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return command.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Ошибка выполнения скалярного запроса: {query}", ex);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Асинхронно выполняет SQL-запрос и возвращает скалярное значение
        public async Task<object> ExecuteScalarAsync(string query, params SqlParameter[] parameters)
        {
            try
            {
                await OpenConnectionAsync();

                using (var command = new SqlCommand(query, Connection))
                {
                    command.CommandTimeout = _commandTimeout;

                    if (parameters != null && parameters.Length > 0)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return await command.ExecuteScalarAsync();
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Асинхронная ошибка скалярного запроса: {query}", ex);
            }
            finally
            {
                CloseConnection();
            }
        }

        // Начинает транзакцию
        public SqlTransaction BeginTransaction()
        {
            OpenConnection();
            return Connection.BeginTransaction();
        }

        // Начинает транзакцию с указанным уровнем изоляции
        public SqlTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            OpenConnection();
            return Connection.BeginTransaction(isolationLevel);
        }

        // Реализация интерфейса IDisposable для освобождения ресурсов
        public void Dispose()
        {
            Dispose(true);
            // Запрещаем финализатору вызывать Dispose повторно
            GC.SuppressFinalize(this);
        }

        // Защищенный метод для освобождения ресурсов
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    if (_connection != null)
                    {
                        CloseConnection();
                        _connection.Dispose();
                        _connection = null;
                    }
                }
                _disposed = true;
            }
        }
        // Финализатор
        ~DatabaseConnection()
        {
            Dispose(false);
        }
       
    }

    // Специализированное исключение для ошибок базы данных
    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}