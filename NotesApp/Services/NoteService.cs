using System;
using System.Collections.Generic;
using NotesApp.Models;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Выполняет операции с заметками пользователя.
    /// </summary>
    public class NoteService
    {
        private const string _insertNoteSql =
            "INSERT INTO notes (owner_id, content) " +
            "VALUES (@ownerId, @content) " +
            "RETURNING id, owner_id, content, created_at, modified_at;";

        private const string _listNotesSql =
            "SELECT id, owner_id, content, created_at, modified_at " +
            "FROM notes " +
            "WHERE owner_id = @ownerId " +
            "ORDER BY created_at DESC, id DESC;";

        private const string _findUserIdByUsernameSql =
            "SELECT id " +
            "FROM app_users " +
            "WHERE username = @username;";

        private const string _listUserNotesSql =
            "SELECT n.id, n.owner_id, u.username, n.content, n.created_at, n.modified_at " +
            "FROM notes n " +
            "JOIN app_users u ON u.id = n.owner_id " +
            "WHERE u.username = @username " +
            "ORDER BY n.created_at DESC, n.id DESC;";

        private const string _deleteNoteSql =
            "DELETE FROM notes " +
            "WHERE id = @id AND owner_id = @ownerId;";

        private const string _updateNoteSql =
            "UPDATE notes " +
            "SET content = @content, modified_at = NOW() " +
            "WHERE id = @id AND owner_id = @ownerId;";

        private readonly DbConnectionProvider _connectionProvider;

        /// <summary>
        /// Создает сервис работы с заметками.
        /// </summary>
        /// <param name="connectionProvider">Поставщик подключений к базе данных.</param>
        public NoteService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Добавляет новую заметку текущего пользователя.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="content">Текст заметки.</param>
        public NoteRecord AddNote(AppUser user, string content)
        {
            CheckUser(user);
            CheckContent(content);

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_insertNoteSql, connection))
            {
                command.Parameters.AddWithValue("ownerId", user.Id);
                command.Parameters.AddWithValue("content", content);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return ReadNote(reader);
                    }
                }
            }

            throw new InvalidOperationException("Не удалось добавить заметку.");
        }

        /// <summary>
        /// Возвращает список заметок текущего пользователя.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        public List<NoteRecord> GetNotes(AppUser user)
        {
            CheckUser(user);

            List<NoteRecord> notes = new List<NoteRecord>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_listNotesSql, connection))
            {
                command.Parameters.AddWithValue("ownerId", user.Id);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        notes.Add(ReadNote(reader));
                    }
                }
            }

            return notes;
        }

        /// <summary>
        /// Возвращает список заметок указанного пользователя для администратора.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="username">Логин пользователя, чьи заметки нужно показать.</param>
        public List<NoteRecord> GetUserNotes(AppUser user, string username)
        {
            CheckAdmin(user);
            CheckUsername(username);

            List<NoteRecord> notes = new List<NoteRecord>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            {
                if (!UserExists(connection, username))
                {
                    throw new InvalidOperationException("Пользователь не найден.");
                }

                using (NpgsqlCommand command = new NpgsqlCommand(_listUserNotesSql, connection))
                {
                    command.Parameters.AddWithValue("username", username);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            notes.Add(ReadNoteWithOwner(reader));
                        }
                    }
                }
            }

            return notes;
        }

        /// <summary>
        /// Удаляет заметку текущего пользователя.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="noteId">Идентификатор заметки.</param>
        public bool DeleteNote(AppUser user, int noteId)
        {
            CheckUser(user);

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_deleteNoteSql, connection))
            {
                command.Parameters.AddWithValue("id", noteId);
                command.Parameters.AddWithValue("ownerId", user.Id);

                return command.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Изменяет заметку текущего пользователя.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="noteId">Идентификатор заметки.</param>
        /// <param name="content">Новый текст заметки.</param>
        public bool UpdateNote(AppUser user, int noteId, string content)
        {
            CheckUser(user);
            CheckContent(content);

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_updateNoteSql, connection))
            {
                command.Parameters.AddWithValue("id", noteId);
                command.Parameters.AddWithValue("ownerId", user.Id);
                command.Parameters.AddWithValue("content", content);

                return command.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Преобразует строку результата запроса в заметку.
        /// </summary>
        /// <param name="reader">Объект чтения данных PostgreSQL.</param>
        private static NoteRecord ReadNote(NpgsqlDataReader reader)
        {
            return new NoteRecord
            {
                Id = reader.GetInt32(0),
                OwnerId = reader.GetInt32(1),
                Content = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),
                ModifiedAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4)
            };
        }

        /// <summary>
        /// Преобразует строку результата запроса в заметку с логином владельца.
        /// </summary>
        /// <param name="reader">Объект чтения данных PostgreSQL.</param>
        private static NoteRecord ReadNoteWithOwner(NpgsqlDataReader reader)
        {
            return new NoteRecord
            {
                Id = reader.GetInt32(0),
                OwnerId = reader.GetInt32(1),
                OwnerUsername = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                ModifiedAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
            };
        }

        /// <summary>
        /// Проверяет, что пользователь авторизован.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        private static void CheckUser(AppUser user)
        {
            if (user == null)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }
        }

        /// <summary>
        /// Проверяет, что пользователь является администратором.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        private static void CheckAdmin(AppUser user)
        {
            CheckUser(user);

            if (user.RoleCode != "admin")
            {
                throw new InvalidOperationException("Команда доступна только администратору.");
            }
        }

        /// <summary>
        /// Проверяет логин пользователя.
        /// </summary>
        /// <param name="username">Логин пользователя.</param>
        private static void CheckUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Логин пользователя не может быть пустым.");
            }
        }

        /// <summary>
        /// Проверяет текст заметки.
        /// </summary>
        /// <param name="content">Текст заметки.</param>
        private static void CheckContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Текст заметки не может быть пустым.");
            }
        }

        /// <summary>
        /// Проверяет существование пользователя по логину.
        /// </summary>
        /// <param name="connection">Открытое подключение к базе данных.</param>
        /// <param name="username">Логин пользователя.</param>
        private static bool UserExists(NpgsqlConnection connection, string username)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(_findUserIdByUsernameSql, connection))
            {
                command.Parameters.AddWithValue("username", username);
                object result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }
    }
}
