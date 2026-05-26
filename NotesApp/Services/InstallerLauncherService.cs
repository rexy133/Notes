using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NotesApp.Services
{
    /// <summary>
    /// Запускает Installer.exe для применения скачанного обновления.
    /// </summary>
    public class InstallerLauncherService
    {
        private const string _installerExeName = "Installer.exe";

        /// <summary>
        /// Запускает установщик для применения скачанного обновления.
        /// </summary>
        /// <param name="archivePath">Путь к zip-архиву обновления.</param>
        public void Launch(string archivePath)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                throw new FileNotFoundException("Архив обновления не найден.", archivePath);
            }

            string installerPath = FindInstallerPath();
            string applicationFolder = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
            string restartExeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                WorkingDirectory = Path.GetDirectoryName(installerPath),
                Arguments = Quote(archivePath) + " " + Quote(applicationFolder) + " " + Quote(restartExeName),
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }

        /// <summary>
        /// Ищет Installer.exe рядом с приложением или в папках сборки.
        /// </summary>
        private static string FindInstallerPath()
        {
            List<string> paths = GetPossibleInstallerPaths();

            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException("Installer.exe не найден. Сначала соберите проект Installer.");
        }

        /// <summary>
        /// Возвращает возможные пути к Installer.exe.
        /// </summary>
        private static List<string> GetPossibleInstallerPaths()
        {
            string applicationFolder = AppDomain.CurrentDomain.BaseDirectory;
            string solutionFolder = Path.GetFullPath(Path.Combine(applicationFolder, @"..\..\.."));

            return new List<string>
            {
                Path.Combine(applicationFolder, _installerExeName),
                Path.Combine(solutionFolder, @"Installer\bin\Debug", _installerExeName),
                Path.Combine(solutionFolder, @"Installer\bin\Release", _installerExeName)
            };
        }

        /// <summary>
        /// Оборачивает значение в кавычки для командной строки.
        /// </summary>
        /// <param name="value">Значение аргумента командной строки.</param>
        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
