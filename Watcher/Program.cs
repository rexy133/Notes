using System;
using System.Configuration;
using System.Text;
using System.Threading;
using Watcher.Models;
using Watcher.Services;

namespace Watcher
{
    internal class Program
    {
        private const string _connectionName = "WatcherDb";
        private const string _intervalSettingName = "WatcherIntervalSeconds";
        private const int _defaultIntervalSeconds = 60;
        private const int _minimumIntervalSeconds = 5;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Notes Watcher";

            int intervalSeconds = ReadIntervalSeconds(args);
            string connectionString = ReadConnectionString();

            MetricCollector collector = new MetricCollector();
            WatcherMetricSender sender = new WatcherMetricSender(connectionString);

            Console.WriteLine("Notes Watcher запущен.");
            Console.WriteLine("Компьютер: " + Environment.MachineName);
            Console.WriteLine("Интервал отправки: " + intervalSeconds + " сек.");
            Console.WriteLine("Для остановки нажмите Ctrl+C.");
            Console.WriteLine();

            while (true)
            {
                try
                {
                    WatcherMetric metric = collector.Collect();
                    sender.Send(metric);
                    PrintMetric(metric);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                                      " | Ошибка watcher-а: " + ex.Message);
                }

                Thread.Sleep(intervalSeconds * 1000);
            }
        }

        private static int ReadIntervalSeconds(string[] args)
        {
            if (args != null && args.Length > 0 && int.TryParse(args[0], out int intervalFromArgs))
            {
                return NormalizeInterval(intervalFromArgs);
            }

            string value = ConfigurationManager.AppSettings[_intervalSettingName];
            if (int.TryParse(value, out int intervalFromConfig))
            {
                return NormalizeInterval(intervalFromConfig);
            }

            return _defaultIntervalSeconds;
        }

        private static int NormalizeInterval(int intervalSeconds)
        {
            return intervalSeconds < _minimumIntervalSeconds ? _minimumIntervalSeconds : intervalSeconds;
        }

        private static string ReadConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[_connectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException("В App.config не настроена строка подключения " + _connectionName + ".");
            }

            return settings.ConnectionString;
        }

        private static void PrintMetric(WatcherMetric metric)
        {
            Console.WriteLine(metric.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss") +
                              " | CPU: " + metric.CpuLoad.ToString("0.00") + "%" +
                              " | RAM: " + metric.RamLoad.ToString("0.00") + "%" +
                              " | HDD: " + metric.DiskLoad.ToString("0.00") + "%");
        }
    }

}
