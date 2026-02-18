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

        public DatabaseConnection()
        {
         
            _commandTimeout = AppConfiguration.GetDatabaseSetting("CommandTimeout", 30);
        }

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

        public int CommandTimeout => _commandTimeout;

        private void InitializeConnection()
        {
            try
            {
                var connectionString = AppConfiguration.GetConnectionString();

                _connection = new SqlConnection(connectionString);

                OpenConnection();

                System.Diagnostics.Debug.WriteLine($"Подключение к БД установлено: {_connection.Database}");
            }
            catch (SqlException sqlEx)
            {
                throw new DatabaseException("Ошибка подключения к базе данных", sqlEx);
            }
            catch (Exception ex)
            {
                throw new DatabaseException("Ошибка инициализации подключения", ex);
            }
        }

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

        public void CloseConnection()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
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
                CloseConnection();
            }
        }

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

        public SqlTransaction BeginTransaction()
        {
            OpenConnection();
            return Connection.BeginTransaction();
        }

        public SqlTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            OpenConnection();
            return Connection.BeginTransaction(isolationLevel);
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

        ~DatabaseConnection()
        {
            Dispose(false);
        }
    }

    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}