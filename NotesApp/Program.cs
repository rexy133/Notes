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
            UserAdminService userAdminService = new UserAdminService(connectionProvider);
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
                    currentUser = RunCommandConsole(currentUser, noteService, userAdminService);
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

        private static AppUser RunCommandConsole(
            AppUser currentUser,
            NoteService noteService,
            UserAdminService userAdminService)
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
                    PrintHelp(currentUser);
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
                else if (IsNoteCommand(command))
                {
                    RunNoteCommand(currentUser, noteService, command);
                }
                else if (IsAdminCommand(command))
                {
                    RunAdminCommand(currentUser, userAdminService, command);
                }
                else
                {
                    Console.WriteLine("Неизвестная команда. Введите help для справки.");
                }

                Console.WriteLine();
            }
        }

        private static void PrintHelp(AppUser user)
        {
            Console.WriteLine();
            Console.WriteLine("Доступные команды:");
            Console.WriteLine("help                                показать список команд");
            Console.WriteLine("logout                              выйти из учетной записи");
            Console.WriteLine("exit                                закрыть приложение");

            if (CanUseNotes(user))
            {
                Console.WriteLine("addNote <текст>                     добавить заметку");
                Console.WriteLine("listNotes                           показать свои заметки");
                Console.WriteLine("editNote <id> <новый текст>         изменить заметку");
                Console.WriteLine("deleteNote <id>                     удалить заметку");
            }

            if (IsAdmin(user))
            {
                Console.WriteLine("createUser <логин> <пароль> <роль>  создать пользователя");
                Console.WriteLine("listUsers                           показать пользователей");
                Console.WriteLine("blockUser <логин>                   заблокировать пользователя");
                Console.WriteLine("unblockUser <логин>                 разблокировать пользователя");
                Console.WriteLine("deleteUser <логин>                  удалить пользователя");
                Console.WriteLine("Роли для createUser: user, admin, analyst");
            }
        }

        private static bool IsNoteCommand(string command)
        {
            return command.StartsWith("addNote ", StringComparison.OrdinalIgnoreCase) ||
                   command.Equals("listNotes", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("editNote ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("deleteNote ", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdminCommand(string command)
        {
            return command.StartsWith("createUser ", StringComparison.OrdinalIgnoreCase) ||
                   command.Equals("listUsers", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("blockUser ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("unblockUser ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("deleteUser ", StringComparison.OrdinalIgnoreCase);
        }

        private static void RunNoteCommand(AppUser currentUser, NoteService noteService, string command)
        {
            if (!CanUseNotes(currentUser))
            {
                Console.WriteLine("Команды заметок недоступны для вашей роли.");
                return;
            }

            if (command.StartsWith("addNote ", StringComparison.OrdinalIgnoreCase))
            {
                AddNote(currentUser, noteService, command);
            }
            else if (command.Equals("listNotes", StringComparison.OrdinalIgnoreCase))
            {
                PrintNotes(currentUser, noteService);
            }
            else if (command.StartsWith("deleteNote ", StringComparison.OrdinalIgnoreCase))
            {
                DeleteNote(currentUser, noteService, command);
            }
            else if (command.StartsWith("editNote ", StringComparison.OrdinalIgnoreCase))
            {
                EditNote(currentUser, noteService, command);
            }
        }

        private static void RunAdminCommand(AppUser currentUser, UserAdminService userAdminService, string command)
        {
            if (!IsAdmin(currentUser))
            {
                Console.WriteLine("Команда доступна только администратору.");
                return;
            }

            if (command.StartsWith("createUser ", StringComparison.OrdinalIgnoreCase))
            {
                CreateUser(currentUser, userAdminService, command);
            }
            else if (command.Equals("listUsers", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsers(currentUser, userAdminService);
            }
            else if (command.StartsWith("blockUser ", StringComparison.OrdinalIgnoreCase))
            {
                BlockUser(currentUser, userAdminService, command);
            }
            else if (command.StartsWith("unblockUser ", StringComparison.OrdinalIgnoreCase))
            {
                UnblockUser(currentUser, userAdminService, command);
            }
            else if (command.StartsWith("deleteUser ", StringComparison.OrdinalIgnoreCase))
            {
                DeleteUser(currentUser, userAdminService, command);
            }
        }

        private static void AddNote(AppUser currentUser, NoteService noteService, string command)
        {
            string content = command.Substring("addNote ".Length).Trim();

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
            string idText = command.Substring("deleteNote ".Length).Trim();

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
            string arguments = command.Substring("editNote ".Length).Trim();
            int separatorIndex = arguments.IndexOf(' ');

            if (separatorIndex <= 0)
            {
                Console.WriteLine("Формат команды: editNote <id> <новый текст>");
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

        private static void CreateUser(AppUser currentUser, UserAdminService userAdminService, string command)
        {
            string arguments = command.Substring("createUser ".Length).Trim();
            string[] parts = arguments.Split(new[] { ' ' }, 3);

            if (parts.Length < 3)
            {
                Console.WriteLine("Формат команды: createUser <логин> <пароль> <роль>");
                return;
            }

            AuthResult result = userAdminService.CreateUser(currentUser, parts[0], parts[1], parts[2]);
            Console.WriteLine(result.Message);
        }

        private static void PrintUsers(AppUser currentUser, UserAdminService userAdminService)
        {
            try
            {
                List<AppUser> users = userAdminService.GetUsers(currentUser);

                foreach (AppUser user in users)
                {
                    string status = user.Blocked ? "заблокирован" : "активен";
                    Console.WriteLine(user.Id + " | " + user.Username + " | " + user.RoleCode + " | " + status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения пользователей: " + ex.Message);
            }
        }

        private static void BlockUser(AppUser currentUser, UserAdminService userAdminService, string command)
        {
            string username = command.Substring("blockUser ".Length).Trim();
            AuthResult result = userAdminService.BlockUser(currentUser, username);
            Console.WriteLine(result.Message);
        }

        private static void UnblockUser(AppUser currentUser, UserAdminService userAdminService, string command)
        {
            string username = command.Substring("unblockUser ".Length).Trim();
            AuthResult result = userAdminService.UnblockUser(currentUser, username);
            Console.WriteLine(result.Message);
        }

        private static void DeleteUser(AppUser currentUser, UserAdminService userAdminService, string command)
        {
            string username = command.Substring("deleteUser ".Length).Trim();
            AuthResult result = userAdminService.DeleteUser(currentUser, username);
            Console.WriteLine(result.Message);
        }

        private static bool CanUseNotes(AppUser user)
        {
            return user.RoleCode == "user" || user.RoleCode == "admin";
        }

        private static bool IsAdmin(AppUser user)
        {
            return user.RoleCode == "admin";
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
