using System;
using System.Configuration;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Открывает подключения к PostgreSQL по строкам из App.config.
    /// </summary>
    public class DbConnectionProvider
    {
        private const string _authConnectionName = "AuthDb";
        private const string _userConnectionName = "UserDb";
        private const string _adminConnectionName = "AdminDb";
        private const string _analystConnectionName = "AnalystDb";

        /// <summary>
        /// Открывает подключение для входа и регистрации.
        /// </summary>
        public NpgsqlConnection OpenAuthConnection()
        {
            return OpenConnection(_authConnectionName);
        }

        /// <summary>
        /// Открывает подключение с правами роли текущего пользователя.
        /// </summary>
        /// <param name="roleCode">Код роли пользователя.</param>
        public NpgsqlConnection OpenConnectionForRole(string roleCode)
        {
            string connectionName = GetConnectionNameForRole(roleCode);
            return OpenConnection(connectionName);
        }

        /// <summary>
        /// Открывает подключение по имени строки подключения.
        /// </summary>
        /// <param name="connectionName">Имя строки подключения в App.config.</param>
        private static NpgsqlConnection OpenConnection(string connectionName)
        {
            string connectionString = ReadConnectionString(connectionName);
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);

            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Читает строку подключения из конфигурации приложения.
        /// </summary>
        /// <param name="connectionName">Имя строки подключения в App.config.</param>
        private static string ReadConnectionString(string connectionName)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("В App.config не настроена строка подключения " + connectionName + ".");
            }

            return settings.ConnectionString;
        }

        /// <summary>
        /// Возвращает имя строки подключения для роли пользователя.
        /// </summary>
        /// <param name="roleCode">Код роли пользователя.</param>
        private static string GetConnectionNameForRole(string roleCode)
        {
            if (string.Equals(roleCode, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return _adminConnectionName;
            }

            if (string.Equals(roleCode, "analyst", StringComparison.OrdinalIgnoreCase))
            {
                return _analystConnectionName;
            }

            if (string.Equals(roleCode, "user", StringComparison.OrdinalIgnoreCase))
            {
                return _userConnectionName;
            }

            throw new InvalidOperationException("Неизвестная роль пользователя: " + roleCode + ".");
        }
    }
}
