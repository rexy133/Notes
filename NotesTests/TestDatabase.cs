using System;
using System.Configuration;
using NotesApp.Models;
using Npgsql;

namespace NotesTests
{
    public static class TestDatabase
    {
        private const string _testPasswordHash =
            "$2a$11$RHQJbQxFTwLc1KaH7WJigOLvFMVMNmOQR65egg3dqyJWfQvmxm0RG";

        /// <summary>
        /// Очищает тестовые данные и создает набор тестовых пользователей.
        /// </summary>
        public static void Reset()
        {
            using (NpgsqlConnection connection = OpenAdminConnection())
            {
                DeleteTestData(connection);
                EnsureRoles(connection);
                InsertTestUser(connection, "test_user", "user", false);
                InsertTestUser(connection, "test_admin", "admin", false);
                InsertTestUser(connection, "test_analyst", "analyst", false);
                InsertTestUser(connection, "blocked_user", "user", true);
            }
        }

        /// <summary>
        /// Возвращает тестового пользователя по логину.
        /// </summary>
        /// <param name="username">Логин тестового пользователя.</param>
        public static AppUser GetUser(string username)
        {
            using (NpgsqlConnection connection = OpenAdminConnection())
            using (NpgsqlCommand command = new NpgsqlCommand(
                "SELECT u.id, u.username, r.role_code, r.title, u.blocked " +
                "FROM app_users u " +
                "JOIN app_roles r ON r.id = u.role_id " +
                "WHERE u.username = @username;",
                connection))
            {
                command.Parameters.AddWithValue("username", username);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("Тестовый пользователь не найден: " + username);
                    }

                    return new AppUser
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        RoleCode = reader.GetString(2),
                        RoleTitle = reader.GetString(3),
                        Blocked = reader.GetBoolean(4)
                    };
                }
            }
        }

        /// <summary>
        /// Открывает подключение администратора к тестовой базе данных.
        /// </summary>
        private static NpgsqlConnection OpenAdminConnection()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["AdminDb"];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("В App.config тестов не настроена строка AdminDb.");
            }

            NpgsqlConnection connection = new NpgsqlConnection(settings.ConnectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Удаляет данные, созданные предыдущими тестовыми сценариями.
        /// </summary>
        /// <param name="connection">Открытое подключение к тестовой базе.</param>
        private static void DeleteTestData(NpgsqlConnection connection)
        {
            Execute(connection, "DELETE FROM device_metrics;");
            Execute(connection, "DELETE FROM watcher_devices;");
            Execute(connection, "DELETE FROM audit_events;");
            Execute(connection, "DELETE FROM notes;");
            Execute(connection, "DELETE FROM app_users WHERE username IN ('test_user', 'test_admin', 'test_analyst', 'blocked_user');");
        }

        /// <summary>
        /// Создает или обновляет базовые роли приложения в тестовой базе.
        /// </summary>
        /// <param name="connection">Открытое подключение к тестовой базе.</param>
        private static void EnsureRoles(NpgsqlConnection connection)
        {
            Execute(
                connection,
                "INSERT INTO app_roles (role_code, title) VALUES " +
                "('user', 'Пользователь'), " +
                "('admin', 'Администратор'), " +
                "('analyst', 'Аналитик') " +
                "ON CONFLICT (role_code) DO UPDATE SET title = EXCLUDED.title;");
        }

        /// <summary>
        /// Добавляет тестового пользователя с указанной ролью.
        /// </summary>
        /// <param name="connection">Открытое подключение к тестовой базе.</param>
        /// <param name="username">Логин тестового пользователя.</param>
        /// <param name="roleCode">Код роли пользователя.</param>
        /// <param name="blocked">Признак блокировки учетной записи.</param>
        private static void InsertTestUser(NpgsqlConnection connection, string username, string roleCode, bool blocked)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(
                "INSERT INTO app_users (username, password_hash, role_id, blocked) " +
                "VALUES (@username, @passwordHash, (SELECT id FROM app_roles WHERE role_code = @roleCode), @blocked);",
                connection))
            {
                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("passwordHash", _testPasswordHash);
                command.Parameters.AddWithValue("roleCode", roleCode);
                command.Parameters.AddWithValue("blocked", blocked);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Выполняет SQL-команду без возвращаемого результата.
        /// </summary>
        /// <param name="connection">Открытое подключение к тестовой базе.</param>
        /// <param name="sql">SQL-команда для выполнения.</param>
        private static void Execute(NpgsqlConnection connection, string sql)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}
