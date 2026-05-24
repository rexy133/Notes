using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NotesApp.Models;
using NotesApp.Services;

namespace NotesApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Notes";

            DbConnectionProvider connectionProvider = new DbConnectionProvider();
            AuthService authService = new AuthService(connectionProvider);
            AppUser currentUser = null;

            while (true)
            {
                Console.Clear();

                if (currentUser == null)
                {
                    PrintGuestMenu();
                    string command = Console.ReadLine();

                    if (command == "1")
                    {
                        currentUser = Login(authService);
                    }
                    else if (command == "2")
                    {
                        Register(authService);
                    }
                    else if (command == "0")
                    {
                        return;
                    }
                    else
                    {
                        ShowMessage("Неизвестная команда.");
                    }
                }
                else
                {
                    PrintUserMenu(currentUser);
                    string command = Console.ReadLine();

                    if (command == "1")
                    {
                        ShowMessage("Вы вошли как " + currentUser.Username + ". Роль: " + currentUser.RoleTitle + ".");
                    }
                    else if (command == "2")
                    {
                        currentUser = null;
                        ShowMessage("Вы вышли из учетной записи.");
                    }
                    else if (command == "0")
                    {
                        return;
                    }
                    else
                    {
                        ShowMessage("Неизвестная команда.");
                    }
                }
            }
        }

        private static AppUser Login(AuthService authService)
        {
            Console.Clear();
            Console.WriteLine("Вход");
            Console.WriteLine();

            Console.Write("Логин: ");
            string username = Console.ReadLine();

            Console.Write("Пароль: ");
            string password = Console.ReadLine();

            AuthResult result = authService.Login(username, password);
            ShowMessage(result.Message);

            return result.Success ? result.User : null;
        }

        private static void Register(AuthService authService)
        {
            Console.Clear();
            Console.WriteLine("Регистрация");
            Console.WriteLine();

            Console.Write("Логин: ");
            string username = Console.ReadLine();

            Console.Write("Пароль: ");
            string password = Console.ReadLine();

            AuthResult result = authService.Register(username, password);
            ShowMessage(result.Message);
        }

        private static void PrintGuestMenu()
        {
            Console.WriteLine("Notes");
            Console.WriteLine();
            Console.WriteLine("1 - Войти");
            Console.WriteLine("2 - Зарегистрироваться");
            Console.WriteLine("0 - Выход");
            Console.WriteLine();
            Console.Write("Выберите действие: ");
        }

        private static void PrintUserMenu(AppUser user)
        {
            Console.WriteLine("Notes");
            Console.WriteLine("Пользователь: " + user.Username);
            Console.WriteLine("Роль: " + user.RoleTitle);
            Console.WriteLine();
            Console.WriteLine("1 - Показать текущего пользователя");
            Console.WriteLine("2 - Выйти из учетной записи");
            Console.WriteLine("0 - Выход");
            Console.WriteLine();
            Console.Write("Выберите действие: ");
        }

        private static void ShowMessage(string message)
        {
            Console.WriteLine();
            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для продолжения.");
            Console.ReadLine();
        }
    }
}
