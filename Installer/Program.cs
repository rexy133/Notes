using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Installer.Models;
using Installer.Services;

namespace Installer
{
    internal class Program
    {
        /// <summary>
        /// Точка входа установщика обновлений.
        /// </summary>
        /// <param name="args">Аргументы командной строки.</param>
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Notes Installer";

            try
            {
                InstallArguments installArguments = InstallArgumentParser.Parse(args);
                UpdateInstaller installer = new UpdateInstaller();
                installer.Install(installArguments);

                Console.WriteLine("Обновление завершено.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка обновления: " + ex.Message);
                Console.WriteLine();
                Console.WriteLine("Нажмите Enter для выхода.");
                Console.ReadLine();
            }
        }
    }
}
