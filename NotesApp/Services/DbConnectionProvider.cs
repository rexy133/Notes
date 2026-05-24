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
        private const string _statisticianConnectionName = "StatisticianDb";

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
        public NpgsqlConnection OpenConnectionForRole(string roleCode)
        {
            string connectionName = GetConnectionNameForRole(roleCode);
            return OpenConnection(connectionName);
        }

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

        private static string ReadConnectionString(string connectionName)
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("В App.config не настроена строка подключения " + connectionName + ".");
            }

            return settings.ConnectionString;
        }

        private static string GetConnectionNameForRole(string roleCode)
        {
            if (string.Equals(roleCode, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return _adminConnectionName;
            }

            if (string.Equals(roleCode, "statistician", StringComparison.OrdinalIgnoreCase))
            {
                return _statisticianConnectionName;
            }

            if (string.Equals(roleCode, "user", StringComparison.OrdinalIgnoreCase))
            {
                return _userConnectionName;
            }

            throw new InvalidOperationException("Неизвестная роль пользователя: " + roleCode + ".");
        }
    }
}
