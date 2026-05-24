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
            NoteService noteService = new NoteService(connectionProvider);
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
                    currentUser = RunCommandConsole(currentUser, noteService);
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

        private static AppUser RunCommandConsole(AppUser currentUser, NoteService noteService)
        {
            Console.Clear();
            Console.WriteLine("Вход выполнен.");
            Console.WriteLine("Пользователь: " + currentUser.Username);
            Console.WriteLine("Роль: " + currentUser.RoleTitle);
            Console.WriteLine("Введите help для просмотра команд.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("notes> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string command = input.Trim();

                if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                }
                else if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Environment.Exit(0);
                }
                else if (command.Equals("logout", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Вы вышли из учетной записи.");
                    Pause();
                    return null;
                }
                else if (command.StartsWith("note add ", StringComparison.OrdinalIgnoreCase))
                {
                    AddNote(currentUser, noteService, command);
                }
                else if (command.Equals("note list", StringComparison.OrdinalIgnoreCase))
                {
                    PrintNotes(currentUser, noteService);
                }
                else if (command.StartsWith("note delete ", StringComparison.OrdinalIgnoreCase))
                {
                    DeleteNote(currentUser, noteService, command);
                }
                else if (command.StartsWith("note edit ", StringComparison.OrdinalIgnoreCase))
                {
                    EditNote(currentUser, noteService, command);
                }
                else
                {
                    Console.WriteLine("Неизвестная команда. Введите help для справки.");
                }

                Console.WriteLine();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("help                         показать список команд");
            Console.WriteLine("note add <текст>             добавить заметку");
            Console.WriteLine("note list                    показать свои заметки");
            Console.WriteLine("note edit <id> <новый текст> изменить заметку");
            Console.WriteLine("note delete <id>             удалить заметку");
            Console.WriteLine("logout                       выйти из учетной записи");
            Console.WriteLine("exit                         закрыть приложение");
        }

        private static void AddNote(AppUser currentUser, NoteService noteService, string command)
        {
            string content = command.Substring("note add ".Length).Trim();

            try
            {
                NoteRecord note = noteService.AddNote(currentUser, content);
                Console.WriteLine("Заметка добавлена. Id: " + note.Id + ".");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка добавления заметки: " + ex.Message);
            }
        }

        private static void PrintNotes(AppUser currentUser, NoteService noteService)
        {
            try
            {
                List<NoteRecord> notes = noteService.GetNotes(currentUser);

                if (notes.Count == 0)
                {
                    Console.WriteLine("Заметок пока нет.");
                    return;
                }

                foreach (NoteRecord note in notes)
                {
                    Console.WriteLine(note.Id + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm") + " | " + note.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения заметок: " + ex.Message);
            }
        }

        private static void DeleteNote(AppUser currentUser, NoteService noteService, string command)
        {
            string idText = command.Substring("note delete ".Length).Trim();

            if (!int.TryParse(idText, out int noteId))
            {
                Console.WriteLine("Укажите числовой id заметки.");
                return;
            }

            try
            {
                bool deleted = noteService.DeleteNote(currentUser, noteId);
                Console.WriteLine(deleted ? "Заметка удалена." : "Заметка не найдена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка удаления заметки: " + ex.Message);
            }
        }

        private static void EditNote(AppUser currentUser, NoteService noteService, string command)
        {
            string arguments = command.Substring("note edit ".Length).Trim();
            int separatorIndex = arguments.IndexOf(' ');

            if (separatorIndex <= 0)
            {
                Console.WriteLine("Формат команды: note edit <id> <новый текст>");
                return;
            }

            string idText = arguments.Substring(0, separatorIndex);
            string content = arguments.Substring(separatorIndex + 1).Trim();

            if (!int.TryParse(idText, out int noteId))
            {
                Console.WriteLine("Укажите числовой id заметки.");
                return;
            }

            try
            {
                bool updated = noteService.UpdateNote(currentUser, noteId, content);
                Console.WriteLine(updated ? "Заметка изменена." : "Заметка не найдена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка изменения заметки: " + ex.Message);
            }
        }

        private static void ShowMessage(string message)
        {
            Console.WriteLine();
            Console.WriteLine(message);
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для продолжения.");
            Console.ReadLine();
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для продолжения.");
            Console.ReadLine();
        }
    }
}
