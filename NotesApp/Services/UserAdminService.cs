using System;
using System.Collections.Generic;
using NotesApp.Models;
using NotesApp.Security;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Выполняет администрирование учетных записей.
    /// </summary>
    public class UserAdminService
    {
        private const int _minPasswordLength = 6;

        private const string _checkUserSql =
            "SELECT COUNT(*) FROM app_users WHERE username = @username;";

        private const string _checkRoleSql =
            "SELECT COUNT(*) FROM app_roles WHERE role_code = @roleCode;";

        private const string _createUserSql =
            "INSERT INTO app_users (username, password_hash, role_id, blocked) " +
            "VALUES (@username, @passwordHash, (SELECT id FROM app_roles WHERE role_code = @roleCode), FALSE);";

        private const string _listUsersSql =
            "SELECT u.id, u.username, r.role_code, r.title, u.blocked " +
            "FROM app_users u " +
            "JOIN app_roles r ON r.id = u.role_id " +
            "ORDER BY u.id;";

        private const string _setBlockedSql =
            "UPDATE app_users SET blocked = @blocked WHERE username = @username;";

        private const string _deleteUserSql =
            "DELETE FROM app_users WHERE username = @username;";

        private const string _insertAuditSql =
            "INSERT INTO audit_events (account_id, account_name, action_code, details, object_name) " +
            "VALUES (@accountId, @accountName, @actionCode, @details, @objectName);";

        private readonly DbConnectionProvider _connectionProvider;

        public UserAdminService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создает пользователя с выбранной ролью.
        /// </summary>
        public AuthResult CreateUser(AppUser admin, string username, string password, string roleCode)
        {
            CheckAdmin(admin);

            if (string.IsNullOrWhiteSpace(username))
            {
                return Fail("Логин не может быть пустым.");
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < _minPasswordLength)
            {
                return Fail("Пароль должен содержать минимум " + _minPasswordLength + " символов.");
            }

            if (string.IsNullOrWhiteSpace(roleCode))
            {
                return Fail("Роль не может быть пустой.");
            }

            try
            {
                using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(admin.RoleCode))
                {
                    if (UserExists(connection, username))
                    {
                        return Fail("Пользователь с таким логином уже существует.");
                    }

                    if (!RoleExists(connection, roleCode))
                    {
                        return Fail("Неизвестная роль. Доступные роли: user, admin, analyst.");
                    }

                    string passwordHash = PasswordHasher.CreateHash(password);

                    using (NpgsqlCommand command = new NpgsqlCommand(_createUserSql, connection))
                    {
                        command.Parameters.AddWithValue("username", username);
                        command.Parameters.AddWithValue("passwordHash", passwordHash);
                        command.Parameters.AddWithValue("roleCode", roleCode);
                        command.ExecuteNonQuery();
                    }

                    SaveAuditEvent(connection, admin, "create_user", "Создан пользователь с ролью " + roleCode + ".", username);
                    return Success("Пользователь создан.");
                }
            }
            catch (Exception ex)
            {
                return Fail("Ошибка создания пользователя: " + ex.Message);
            }
        }

        /// <summary>
        /// Возвращает список пользователей.
        /// </summary>
        public List<AppUser> GetUsers(AppUser admin)
        {
            CheckAdmin(admin);

            List<AppUser> users = new List<AppUser>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(admin.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_listUsersSql, connection))
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    users.Add(new AppUser
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        RoleCode = reader.GetString(2),
                        RoleTitle = reader.GetString(3),
                        Blocked = reader.GetBoolean(4)
                    });
                }
            }

            return users;
        }

        /// <summary>
        /// Блокирует пользователя.
        /// </summary>
        public AuthResult BlockUser(AppUser admin, string username)
        {
            return SetBlocked(admin, username, true, "Пользователь заблокирован.", "block_user");
        }

        /// <summary>
        /// Разблокирует пользователя.
        /// </summary>
        public AuthResult UnblockUser(AppUser admin, string username)
        {
            return SetBlocked(admin, username, false, "Пользователь разблокирован.", "unblock_user");
        }

        /// <summary>
        /// Удаляет пользователя.
        /// </summary>
        public AuthResult DeleteUser(AppUser admin, string username)
        {
            CheckAdmin(admin);

            if (string.IsNullOrWhiteSpace(username))
            {
                return Fail("Укажите логин пользователя.");
            }

            if (username == admin.Username)
            {
                return Fail("Нельзя удалить текущую учетную запись.");
            }

            try
            {
                using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(admin.RoleCode))
                using (NpgsqlCommand command = new NpgsqlCommand(_deleteUserSql, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        return Fail("Пользователь не найден.");
                    }

                    SaveAuditEvent(connection, admin, "delete_user", "Удален пользователь.", username);
                    return Success("Пользователь удален.");
                }
            }
            catch (Exception ex)
            {
                return Fail("Ошибка удаления пользователя: " + ex.Message);
            }
        }

        private AuthResult SetBlocked(AppUser admin, string username, bool blocked, string successMessage, string actionCode)
        {
            CheckAdmin(admin);

            if (string.IsNullOrWhiteSpace(username))
            {
                return Fail("Укажите логин пользователя.");
            }

            if (username == admin.Username)
            {
                return Fail("Нельзя изменить блокировку текущей учетной записи.");
            }

            try
            {
                using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(admin.RoleCode))
                using (NpgsqlCommand command = new NpgsqlCommand(_setBlockedSql, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    command.Parameters.AddWithValue("blocked", blocked);
                    int affectedRows = command.ExecuteNonQuery();

                    if (affectedRows == 0)
                    {
                        return Fail("Пользователь не найден.");
                    }

                    SaveAuditEvent(connection, admin, actionCode, successMessage, username);
                    return Success(successMessage);
                }
            }
            catch (Exception ex)
            {
                return Fail("Ошибка изменения пользователя: " + ex.Message);
            }
        }

        private static bool UserExists(NpgsqlConnection connection, string username)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_checkUserSql, connection))
            {
                command.Parameters.AddWithValue("username", username);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool RoleExists(NpgsqlConnection connection, string roleCode)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_checkRoleSql, connection))
            {
                command.Parameters.AddWithValue("roleCode", roleCode);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static void SaveAuditEvent(
            NpgsqlConnection connection,
            AppUser admin,
            string actionCode,
            string details,
            string objectName)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_insertAuditSql, connection))
            {
                command.Parameters.AddWithValue("accountId", admin.Id);
                command.Parameters.AddWithValue("accountName", admin.Username);
                command.Parameters.AddWithValue("actionCode", actionCode);
                command.Parameters.AddWithValue("details", details);
                command.Parameters.AddWithValue("objectName", objectName);
                command.ExecuteNonQuery();
            }
        }

        private static void CheckAdmin(AppUser user)
        {
            if (user == null || user.RoleCode != "admin")
            {
                throw new InvalidOperationException("Команда доступна только администратору.");
            }
        }

        private static AuthResult Success(string message)
        {
            return new AuthResult
            {
                Success = true,
                Message = message
            };
        }

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
