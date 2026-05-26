using System;
using NotesApp.Models;
using NotesApp.Security;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Выполняет регистрацию и вход пользователей.
    /// </summary>
    public class AuthService
    {
        private const int _minPasswordLength = 6;

        private const string _findUserSql =
            "SELECT u.id, u.username, u.password_hash, u.blocked, r.role_code, r.title " +
            "FROM app_users u " +
            "JOIN app_roles r ON r.id = u.role_id " +
            "WHERE u.username = @username;";

        private const string _insertUserSql =
            "INSERT INTO app_users (username, password_hash, role_id, blocked) " +
            "VALUES (@username, @passwordHash, (SELECT id FROM app_roles WHERE role_code = 'user'), FALSE) " +
            "RETURNING id;";

        private const string _insertAuditSql =
            "INSERT INTO audit_events (account_id, account_name, action_code, details, object_name) " +
            "VALUES (@accountId, @accountName, @actionCode, @details, @objectName);";

        private readonly DbConnectionProvider _connectionProvider;

        /// <summary>
        /// Создает сервис авторизации.
        /// </summary>
        /// <param name="connectionProvider">Поставщик подключений к базе данных.</param>
        public AuthService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Регистрирует нового обычного пользователя.
        /// </summary>
        /// <param name="username">Логин нового пользователя.</param>
        /// <param name="password">Пароль нового пользователя.</param>
        public AuthResult Register(string username, string password)
        {
            AuthResult validationResult = ValidateCredentials(username, password);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            try
            {
                using (NpgsqlConnection connection = _connectionProvider.OpenAuthConnection())
                {
                    string existingHash;
                    if (FindUser(connection, username, out existingHash) != null)
                    {
                        return Fail("Пользователь с таким логином уже существует.");
                    }

                    string passwordHash = PasswordHasher.CreateHash(password);
                    int userId = CreateUser(connection, username, passwordHash);

                    SaveAuditEvent(
                        connection,
                        userId,
                        username,
                        "register_success",
                        "Зарегистрирован новый пользователь.",
                        username);

                    return Success("Пользователь успешно зарегистрирован.", null);
                }
            }
            catch (Exception ex)
            {
                return Fail("Ошибка регистрации: " + ex.Message);
            }
        }

        /// <summary>
        /// Выполняет вход пользователя по логину и паролю.
        /// </summary>
        /// <param name="username">Логин пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        public AuthResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return Fail("Введите логин и пароль.");
            }

            try
            {
                using (NpgsqlConnection connection = _connectionProvider.OpenAuthConnection())
                {
                    string passwordHash;
                    AppUser user = FindUser(connection, username, out passwordHash);

                    if (user == null)
                    {
                        SaveAuditEvent(connection, null, username, "login_failed", "Пользователь не найден.", username);
                        return Fail("Пользователь не найден.");
                    }

                    if (user.Blocked)
                    {
                        SaveAuditEvent(connection, user.Id, user.Username, "login_failed", "Учетная запись заблокирована.", user.Username);
                        return Fail("Учетная запись заблокирована.");
                    }

                    if (!PasswordHasher.Verify(password, passwordHash))
                    {
                        SaveAuditEvent(connection, user.Id, user.Username, "login_failed", "Введен неверный пароль.", user.Username);
                        return Fail("Неверный пароль.");
                    }

                    SaveAuditEvent(connection, user.Id, user.Username, "login_success", "Пользователь вошел в систему.", user.Username);
                    return Success("Вход выполнен успешно.", user);
                }
            }
            catch (Exception ex)
            {
                return Fail("Ошибка входа: " + ex.Message);
            }
        }

        /// <summary>
        /// Проверяет логин и пароль перед регистрацией.
        /// </summary>
        /// <param name="username">Логин пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        private static AuthResult ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Fail("Логин не может быть пустым.");
            }

            if (username.Length < 3)
            {
                return Fail("Логин должен содержать минимум 3 символа.");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return Fail("Пароль не может быть пустым.");
            }

            if (password.Length < _minPasswordLength)
            {
                return Fail("Пароль должен содержать минимум " + _minPasswordLength + " символов.");
            }

            return Success("Данные корректны.", null);
        }

        /// <summary>
        /// Ищет пользователя по логину и возвращает его хэш пароля.
        /// </summary>
        /// <param name="connection">Открытое подключение к базе данных.</param>
        /// <param name="username">Логин пользователя.</param>
        /// <param name="passwordHash">Хэш пароля найденного пользователя.</param>
        private static AppUser FindUser(NpgsqlConnection connection, string username, out string passwordHash)
        {
            passwordHash = null;

            using (NpgsqlCommand command = new NpgsqlCommand(_findUserSql, connection))
            {
                command.Parameters.AddWithValue("username", username);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    passwordHash = reader.GetString(2);

                    return new AppUser
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Blocked = reader.GetBoolean(3),
                        RoleCode = reader.GetString(4),
                        RoleTitle = reader.GetString(5)
                    };
                }
            }
        }

        /// <summary>
        /// Создает обычного пользователя в базе данных.
        /// </summary>
        /// <param name="connection">Открытое подключение к базе данных.</param>
        /// <param name="username">Логин пользователя.</param>
        /// <param name="passwordHash">Хэш пароля пользователя.</param>
        private static int CreateUser(NpgsqlConnection connection, string username, string passwordHash)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_insertUserSql, connection))
            {
                command.Parameters.AddWithValue("username", username);
                command.Parameters.AddWithValue("passwordHash", passwordHash);

                object result = command.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        /// <summary>
        /// Сохраняет событие аудита авторизации.
        /// </summary>
        /// <param name="connection">Открытое подключение к базе данных.</param>
        /// <param name="accountId">Идентификатор учетной записи.</param>
        /// <param name="accountName">Логин учетной записи.</param>
        /// <param name="actionCode">Код действия.</param>
        /// <param name="details">Описание события.</param>
        /// <param name="objectName">Название объекта события.</param>
        private static void SaveAuditEvent(
            NpgsqlConnection connection,
            int? accountId,
            string accountName,
            string actionCode,
            string details,
            string objectName)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_insertAuditSql, connection))
            {
                command.Parameters.AddWithValue("accountId", accountId.HasValue ? (object)accountId.Value : DBNull.Value);
                command.Parameters.AddWithValue("accountName", string.IsNullOrWhiteSpace(accountName) ? (object)DBNull.Value : accountName);
                command.Parameters.AddWithValue("actionCode", actionCode);
                command.Parameters.AddWithValue("details", details);
                command.Parameters.AddWithValue("objectName", string.IsNullOrWhiteSpace(objectName) ? (object)DBNull.Value : objectName);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Формирует успешный результат авторизации.
        /// </summary>
        /// <param name="message">Сообщение для пользователя.</param>
        /// <param name="user">Пользователь приложения.</param>
        private static AuthResult Success(string message, AppUser user)
        {
            return new AuthResult
            {
                Success = true,
                Message = message,
                User = user
            };
        }

        /// <summary>
        /// Формирует неуспешный результат авторизации.
        /// </summary>
        /// <param name="message">Сообщение об ошибке.</param>
        private static AuthResult Fail(string message)
        {
            return new AuthResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
