using Npgsql;
using Watcher.Models;

namespace Watcher.Services
{
    public class WatcherMetricSender
    {
        private const string _sendMetricSql =
            "SELECT record_watcher_metric(@deviceUid, @displayName, @cpuLoad, @ramLoad, @diskLoad);";

        private readonly string _connectionString;

        public WatcherMetricSender(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Send(WatcherMetric metric)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            using (NpgsqlCommand command = new NpgsqlCommand(_sendMetricSql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("deviceUid", metric.DeviceUid);
                command.Parameters.AddWithValue("displayName", metric.DisplayName);
                command.Parameters.AddWithValue("cpuLoad", metric.CpuLoad);
                command.Parameters.AddWithValue("ramLoad", metric.RamLoad);
                command.Parameters.AddWithValue("diskLoad", metric.DiskLoad);

                command.ExecuteNonQuery();
            }
        }
    }
}
