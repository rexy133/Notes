using Npgsql;
using Watcher.Models;

namespace Watcher.Services
{
    public class WatcherMetricSender
    {
        private const string _sendMetricSql =
            "SELECT record_watcher_metric(@deviceUid, @cpuLoad, @ramLoad, @diskLoad);";

        private readonly string _connectionString;

        /// <summary>
        /// Создает отправитель метрик watcher-а.
        /// </summary>
        /// <param name="connectionString">Строка подключения к базе данных.</param>
        public WatcherMetricSender(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Отправляет метрику устройства в базу данных.
        /// </summary>
        /// <param name="metric">Метрика watcher-а для сохранения.</param>
        public void Send(WatcherMetric metric)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            using (NpgsqlCommand command = new NpgsqlCommand(_sendMetricSql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("deviceUid", metric.DeviceUid);
                command.Parameters.AddWithValue("cpuLoad", metric.CpuLoad);
                command.Parameters.AddWithValue("ramLoad", metric.RamLoad);
                command.Parameters.AddWithValue("diskLoad", metric.DiskLoad);

                command.ExecuteNonQuery();
            }
        }
    }
}
