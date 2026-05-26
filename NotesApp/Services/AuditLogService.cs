using System;
using System.Collections.Generic;
using NotesApp.Models;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Читает журнал действий для администратора.
    /// </summary>
    public class AuditLogService
    {
        private const int _defaultLimit = 10;
        private const int _maxLimit = 100;

        private const string _listLogsSql =
            "SELECT id, account_id, account_name, action_code, details, object_name, event_time " +
            "FROM audit_events " +
            "ORDER BY event_time DESC, id DESC " +
            "LIMIT @limit;";

        private readonly DbConnectionProvider _connectionProvider;

        /// <summary>
        /// Создает сервис чтения журнала действий.
        /// </summary>
        /// <param name="connectionProvider">Поставщик подключений к базе данных.</param>
        public AuditLogService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Возвращает последние записи журнала действий.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="limit">Количество записей для вывода.</param>
        public List<AuditEventRecord> GetLastLogs(AppUser user, int limit)
        {
            CheckAdmin(user);

            if (limit <= 0)
            {
                limit = _defaultLimit;
            }

            if (limit > _maxLimit)
            {
                limit = _maxLimit;
            }

            List<AuditEventRecord> logs = new List<AuditEventRecord>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_listLogsSql, connection))
            {
                command.Parameters.AddWithValue("limit", limit);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(ReadLog(reader));
                    }
                }
            }

            return logs;
        }

        /// <summary>
        /// Преобразует строку результата запроса в запись журнала.
        /// </summary>
        /// <param name="reader">Объект чтения данных PostgreSQL.</param>
        private static AuditEventRecord ReadLog(NpgsqlDataReader reader)
        {
            return new AuditEventRecord
            {
                Id = reader.GetInt32(0),
                AccountId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                AccountName = reader.IsDBNull(2) ? null : reader.GetString(2),
                ActionCode = reader.GetString(3),
                Details = reader.GetString(4),
                ObjectName = reader.IsDBNull(5) ? null : reader.GetString(5),
                EventTime = reader.GetDateTime(6)
            };
        }

        /// <summary>
        /// Проверяет права администратора.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        private static void CheckAdmin(AppUser user)
        {
            if (user == null || user.RoleCode != "admin")
            {
                throw new InvalidOperationException("Команда доступна только администратору.");
            }
        }
    }
}
