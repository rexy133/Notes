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

        private const string _deleteNoteSql =
            "DELETE FROM notes " +
            "WHERE id = @id AND owner_id = @ownerId;";

        private const string _updateNoteSql =
            "UPDATE notes " +
            "SET content = @content, modified_at = NOW() " +
            "WHERE id = @id AND owner_id = @ownerId;";

        private readonly DbConnectionProvider _connectionProvider;

        public NoteService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Добавляет новую заметку текущего пользователя.
        /// </summary>
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
        /// Удаляет заметку текущего пользователя.
        /// </summary>
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

        private static void CheckUser(AppUser user)
        {
            if (user == null)
            {
                throw new InvalidOperationException("Пользователь не авторизован.");
            }
        }

        private static void CheckContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("Текст заметки не может быть пустым.");
            }
        }
    }
}
