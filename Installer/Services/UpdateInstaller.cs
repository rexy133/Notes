using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Installer.Models;

namespace Installer.Services
{
    public class UpdateInstaller
    {
        private const int _waitDelayMilliseconds = 1000;

        /// <summary>
        /// Устанавливает обновление из zip-архива.
        /// </summary>
        /// <param name="arguments">Параметры установки обновления.</param>
        public void Install(InstallArguments arguments)
        {
            string applicationExePath = Path.Combine(arguments.ApplicationFolder, arguments.RestartExeName);
            string unpackFolder = CreateTemporaryFolder();

            try
            {
                Console.WriteLine("Ожидание закрытия приложения...");
                WaitForApplicationExit(applicationExePath);

                Console.WriteLine("Распаковка архива...");
                ZipFile.ExtractToDirectory(arguments.ArchivePath, unpackFolder);

                Console.WriteLine("Замена файлов...");
                CopyDirectory(GetPackageRootFolder(unpackFolder), arguments.ApplicationFolder);

                Console.WriteLine("Запуск приложения...");
                StartApplication(applicationExePath);
            }
            finally
            {
                DeleteTemporaryFolder(unpackFolder);
            }
        }

        /// <summary>
        /// Создает временную папку для распаковки обновления.
        /// </summary>
        private static string CreateTemporaryFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "NotesUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Ожидает завершения обновляемого приложения.
        /// </summary>
        /// <param name="applicationExePath">Путь к exe-файлу приложения.</param>
        private static void WaitForApplicationExit(string applicationExePath)
        {
            string processName = Path.GetFileNameWithoutExtension(applicationExePath);

            while (IsApplicationRunning(processName, applicationExePath))
            {
                Thread.Sleep(_waitDelayMilliseconds);
            }
        }

        /// <summary>
        /// Проверяет, запущено ли обновляемое приложение.
        /// </summary>
        /// <param name="processName">Имя процесса без расширения.</param>
        /// <param name="applicationExePath">Путь к exe-файлу приложения.</param>
        private static bool IsApplicationRunning(string processName, string applicationExePath)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    string processPath = process.MainModule.FileName;
                    if (string.Equals(processPath, applicationExePath, StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Копирует папку обновления в папку приложения.
        /// </summary>
        /// <param name="sourceFolder">Папка с распакованным обновлением.</param>
        /// <param name="destinationFolder">Папка установленного приложения.</param>
        private static void CopyDirectory(string sourceFolder, string destinationFolder)
        {
            foreach (string folder in Directory.GetDirectories(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string targetFolder = GetTargetPath(sourceFolder, destinationFolder, folder);
                Directory.CreateDirectory(targetFolder);
            }

            foreach (string file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string targetFile = GetTargetPath(sourceFolder, destinationFolder, file);

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                if (IsCurrentInstallerFile(targetFile))
                {
                    continue;
                }

                File.Copy(file, targetFile, true);
            }
        }

        /// <summary>
        /// Определяет корневую папку пакета обновления.
        /// </summary>
        /// <param name="unpackFolder">Папка, в которую распакован архив.</param>
        private static string GetPackageRootFolder(string unpackFolder)
        {
            string[] files = Directory.GetFiles(unpackFolder);
            string[] folders = Directory.GetDirectories(unpackFolder);

            if (files.Length == 0 && folders.Length == 1)
            {
                return folders[0];
            }

            return unpackFolder;
        }

        /// <summary>
        /// Строит путь назначения для файла или папки обновления.
        /// </summary>
        /// <param name="sourceFolder">Корневая папка источника.</param>
        /// <param name="destinationFolder">Корневая папка назначения.</param>
        /// <param name="sourcePath">Исходный путь файла или папки.</param>
        private static string GetTargetPath(string sourceFolder, string destinationFolder, string sourcePath)
        {
            string relativePath = sourcePath.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(destinationFolder, relativePath);
        }

        /// <summary>
        /// Проверяет, является ли файл текущим установщиком.
        /// </summary>
        /// <param name="targetFile">Путь к проверяемому файлу.</param>
        private static bool IsCurrentInstallerFile(string targetFile)
        {
            string currentInstallerPath = Assembly.GetExecutingAssembly().Location;
            return string.Equals(targetFile, currentInstallerPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Запускает приложение после установки обновления.
        /// </summary>
        /// <param name="applicationExePath">Путь к exe-файлу приложения.</param>
        private static void StartApplication(string applicationExePath)
        {
            if (!File.Exists(applicationExePath))
            {
                throw new FileNotFoundException("Exe-файл для запуска не найден.", applicationExePath);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = applicationExePath,
                WorkingDirectory = Path.GetDirectoryName(applicationExePath)
            };

            Process.Start(startInfo);
        }

        /// <summary>
        /// Удаляет временную папку обновления.
        /// </summary>
        /// <param name="folder">Путь к временной папке.</param>
        private static void DeleteTemporaryFolder(string folder)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch
            {
                Console.WriteLine("Не удалось удалить временную папку: " + folder);
            }
        }
    }
}
