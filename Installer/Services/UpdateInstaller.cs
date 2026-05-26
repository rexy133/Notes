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
                CopyDirectory(unpackFolder, arguments.ApplicationFolder);

                Console.WriteLine("Запуск приложения...");
                StartApplication(applicationExePath);
            }
            finally
            {
                DeleteTemporaryFolder(unpackFolder);
            }
        }

        private static string CreateTemporaryFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "NotesUpdate_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void WaitForApplicationExit(string applicationExePath)
        {
            string processName = Path.GetFileNameWithoutExtension(applicationExePath);

            while (IsApplicationRunning(processName, applicationExePath))
            {
                Thread.Sleep(_waitDelayMilliseconds);
            }
        }

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

        private static string GetTargetPath(string sourceFolder, string destinationFolder, string sourcePath)
        {
            string relativePath = sourcePath.Substring(sourceFolder.Length).TrimStart(Path.DirectorySeparatorChar);
            return Path.Combine(destinationFolder, relativePath);
        }

        private static bool IsCurrentInstallerFile(string targetFile)
        {
            string currentInstallerPath = Assembly.GetExecutingAssembly().Location;
            return string.Equals(targetFile, currentInstallerPath, StringComparison.OrdinalIgnoreCase);
        }

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
