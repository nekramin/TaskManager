using System;
using System.IO;
using System.Text.Json;

namespace TaskManagerDesktop.Infrastructure
{
    public static class AppConfiguration
    {
        private static AppSettings _settings;
        private static readonly object _lock = new object();

        public class AppSettings
        {

            public ConnectionStrings ConnectionStrings { get; set; }

            public DatabaseSettings DatabaseSettings { get; set; }

            public ApplicationSettings ApplicationSettings { get; set; }
        }

        public class ConnectionStrings
        {
            public string TaskManagerConnection { get; set; }
        }

        public class DatabaseSettings
        {

            public int CommandTimeout { get; set; }

            public int RetryAttempts { get; set; }

            public bool EnableDetailedErrors { get; set; }
        }

        public class ApplicationSettings
        {

            public string Name { get; set; }
            public string Version { get; set; }
        }

        private static void LoadSettings()
        {
            lock (_lock)
            {
                if (_settings != null) return;

                try
                {
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

                    if (!File.Exists(configPath))
                    {
                        throw new FileNotFoundException($"@aйn конигурашии не нañgeн: {configPath}");
                    }

                    var json = File.ReadAllText(configPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });


                    if (_settings == null)
                    {
                        throw new InvalidOperationException("Не удалось десериализовать настройки");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Ошибка загрузки конфигурации: {ex.Message}", ex);
                }
            }
        }

        public static string GetConnectionString(string name = "TaskManagerConnection")
        {
            LoadSettings();

            if (_settings.ConnectionStrings == null)
            {
                throw new InvalidOperationException("Раздел ConnectionStrings не найден в конфигурации");
            }

            var connectionString = name switch
            {
                "TaskManagerConnection" => _settings.ConnectionStrings.TaskManagerConnection,
                _ => throw new ArgumentException($"Неизвестное имя строки подключения: {name}")
            };

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException($"Строка подключения '{name}' не найдена или пуста");
            }

            return connectionString;
        }

        public static T GetDatabaseSetting<T>(string key, T defaultValue = default)
        {
            LoadSettings();

            if (_settings.DatabaseSettings == null)
            {
                return defaultValue;
            }

            return key switch
            {
                "CommandTimeout" => (T)(object)_settings.DatabaseSettings.CommandTimeout,
                "RetryAttemps" => (T)(object)_settings.DatabaseSettings.RetryAttempts,
                "EnableDetailedErrors" => (T)(object)_settings.DatabaseSettings.EnableDetailedErrors,
                _ => defaultValue
            };
        }

        public static string GetApplicationSetting(string key)
        {
            LoadSettings();

            if (_settings.ApplicationSettings == null)
            {
                return string.Empty;
            }

            return key switch
            {
                "Name" => _settings.ApplicationSettings.Name,
                "Version" => _settings.ApplicationSettings.Version,
                _ => string.Empty
            };
        }

        public static string TestConfiguration()
        {
            try
            {

                LoadSettings();
                var connectionString = GetConnectionString();
                var appName = GetApplicationSetting("Name");
                var timeout = GetDatabaseSetting("CommandTimeout", 30);

                return $"Конфигурация загружена успешно!\n" +
                $"Путь: {AppDomain.CurrentDomain.BaseDirectory}\n" +
                $"Приложение: {appName}\n" +
                $"Таймаут: {timeout} сек.";
            }
            catch (Exception ex)
            {
                return $"Oшибка конфигурации: {ex.Message}";
            }
        }
    }
}