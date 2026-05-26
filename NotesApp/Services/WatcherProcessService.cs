using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NotesApp.Services
{
    /// <summary>
    /// Запускает отдельный процесс Watcher.exe.
    /// </summary>
    public class WatcherProcessService
    {
        private const string _watcherExeName = "Watcher.exe";

        /// <summary>
        /// Запускает watcher с указанным интервалом отправки метрик.
        /// </summary>
        /// <param name="intervalSeconds">Интервал отправки в секундах.</param>
        public string StartWatcher(int? intervalSeconds)
        {
            string watcherPath = FindWatcherPath();

            if (IsWatcherRunning(watcherPath))
            {
                return "Watcher уже запущен.";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = watcherPath,
                WorkingDirectory = Path.GetDirectoryName(watcherPath),
                UseShellExecute = true
            };

            if (intervalSeconds.HasValue)
            {
                startInfo.Arguments = intervalSeconds.Value.ToString();
            }

            Process.Start(startInfo);
            return "Watcher запущен.";
        }

        /// <summary>
        /// Ищет Watcher.exe рядом с приложением или в папках сборки.
        /// </summary>
        private static string FindWatcherPath()
        {
            List<string> paths = GetPossibleWatcherPaths();

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Watcher.exe не найден. Сначала соберите проект Watcher.");
        }

        /// <summary>
        /// Возвращает возможные пути к Watcher.exe.
        /// </summary>
        private static List<string> GetPossibleWatcherPaths()
        {
            string applicationFolder = AppDomain.CurrentDomain.BaseDirectory;
            string solutionFolder = Path.GetFullPath(Path.Combine(applicationFolder, @"..\..\.."));

            return new List<string>
            {
                Path.Combine(applicationFolder, _watcherExeName),
                Path.Combine(solutionFolder, @"Watcher\bin\Debug", _watcherExeName),
                Path.Combine(solutionFolder, @"Watcher\bin\Release", _watcherExeName)
            };
        }

        /// <summary>
        /// Проверяет, запущен ли watcher из указанного пути.
        /// </summary>
        /// <param name="watcherPath">Полный путь к Watcher.exe.</param>
        private static bool IsWatcherRunning(string watcherPath)
        {
            string processName = Path.GetFileNameWithoutExtension(watcherPath);
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    string processPath = process.MainModule.FileName;
                    if (string.Equals(processPath, watcherPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
                finally
                {
                    process.Dispose();
                }
            }

            return false;
        }
    }
}
