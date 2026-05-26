using System;
using System.Collections.Generic;
using NotesApp.Models;
using Npgsql;

namespace NotesApp.Services
{
    /// <summary>
    /// Читает устройства watcher-а и их метрики.
    /// </summary>
    public class WatcherReportService
    {
        private const int _defaultMetricLimit = 10;

        private const string _listDevicesSql =
            "SELECT id, device_uid, network_address, enabled, last_contact_at, added_at " +
            "FROM watcher_devices " +
            "ORDER BY last_contact_at DESC NULLS LAST, id DESC;";

        private const string _listMetricsSql =
            "SELECT id, device_id, cpu_load, ram_load, disk_load, captured_at " +
            "FROM device_metrics " +
            "WHERE device_id = @deviceId " +
            "ORDER BY captured_at DESC, id DESC " +
            "LIMIT @limit;";

        private readonly DbConnectionProvider _connectionProvider;

        /// <summary>
        /// Создает сервис отчетов watcher-а.
        /// </summary>
        /// <param name="connectionProvider">Поставщик подключений к базе данных.</param>
        public WatcherReportService(DbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Возвращает список устройств watcher-а.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        public List<WatcherDeviceRecord> GetDevices(AppUser user)
        {
            CheckAccess(user);

            List<WatcherDeviceRecord> devices = new List<WatcherDeviceRecord>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_listDevicesSql, connection))
            using (NpgsqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    devices.Add(ReadDevice(reader));
                }
            }

            return devices;
        }

        /// <summary>
        /// Возвращает последние метрики устройства с лимитом по умолчанию.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="deviceId">Идентификатор устройства watcher-а.</param>
        public List<DeviceMetricRecord> GetMetrics(AppUser user, int deviceId)
        {
            return GetMetrics(user, deviceId, _defaultMetricLimit);
        }

        /// <summary>
        /// Возвращает последние метрики устройства.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        /// <param name="deviceId">Идентификатор устройства watcher-а.</param>
        /// <param name="limit">Количество метрик для вывода.</param>
        public List<DeviceMetricRecord> GetMetrics(AppUser user, int deviceId, int limit)
        {
            CheckAccess(user);

            if (deviceId <= 0)
            {
                throw new InvalidOperationException("Id устройства должен быть положительным числом.");
            }

            if (limit <= 0)
            {
                limit = _defaultMetricLimit;
            }

            List<DeviceMetricRecord> metrics = new List<DeviceMetricRecord>();

            using (NpgsqlConnection connection = _connectionProvider.OpenConnectionForRole(user.RoleCode))
            using (NpgsqlCommand command = new NpgsqlCommand(_listMetricsSql, connection))
            {
                command.Parameters.AddWithValue("deviceId", deviceId);
                command.Parameters.AddWithValue("limit", limit);

                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        metrics.Add(ReadMetric(reader));
                    }
                }
            }

            return metrics;
        }

        /// <summary>
        /// Преобразует строку результата запроса в устройство watcher-а.
        /// </summary>
        /// <param name="reader">Объект чтения данных PostgreSQL.</param>
        private static WatcherDeviceRecord ReadDevice(NpgsqlDataReader reader)
        {
            return new WatcherDeviceRecord
            {
                Id = reader.GetInt32(0),
                DeviceUid = reader.GetString(1),
                NetworkAddress = reader.IsDBNull(2) ? null : reader.GetString(2),
                Enabled = reader.GetBoolean(3),
                LastContactAt = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                AddedAt = reader.GetDateTime(5)
            };
        }

        /// <summary>
        /// Преобразует строку результата запроса в метрику устройства.
        /// </summary>
        /// <param name="reader">Объект чтения данных PostgreSQL.</param>
        private static DeviceMetricRecord ReadMetric(NpgsqlDataReader reader)
        {
            return new DeviceMetricRecord
            {
                Id = reader.GetInt32(0),
                DeviceId = reader.GetInt32(1),
                CpuLoad = reader.GetDecimal(2),
                RamLoad = reader.GetDecimal(3),
                DiskLoad = reader.GetDecimal(4),
                CapturedAt = reader.GetDateTime(5)
            };
        }

        /// <summary>
        /// Проверяет доступ к командам watcher-а.
        /// </summary>
        /// <param name="user">Текущий пользователь приложения.</param>
        private static void CheckAccess(AppUser user)
        {
            if (user == null || (user.RoleCode != "admin" && user.RoleCode != "analyst"))
            {
                throw new InvalidOperationException("Команды watcher-а доступны только администратору и аналитику.");
            }
        }
    }
}
