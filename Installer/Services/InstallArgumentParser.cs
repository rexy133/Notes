using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Installer.Models;

namespace Installer.Services
{
    public class InstallArgumentParser
    {
        public static InstallArguments Parse(string[] args)
        {
            if (args == null || args.Length < 3)
            {
                throw new InvalidOperationException(
                    "Формат запуска: Installer.exe <путь к zip> <папка приложения> <exe для запуска>.");
            }

            string archivePath = Path.GetFullPath(args[0]);
            string applicationFolder = Path.GetFullPath(args[1]);
            string restartExeName = args[2];

            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException("Архив обновления не найден.", archivePath);
            }

            if (!Directory.Exists(applicationFolder))
            {
                throw new DirectoryNotFoundException("Папка приложения не найдена: " + applicationFolder);
            }

            if (string.IsNullOrWhiteSpace(restartExeName))
            {
                throw new InvalidOperationException("Не указано имя exe-файла для перезапуска.");
            }

            return new InstallArguments
            {
                ArchivePath = archivePath,
                ApplicationFolder = applicationFolder,
                RestartExeName = restartExeName
            };
        }
    }
}
