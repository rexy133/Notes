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
            AuditLogService auditLogService = new AuditLogService(connectionProvider);
            WatcherReportService watcherReportService = new WatcherReportService(connectionProvider);
            WatcherProcessService watcherProcessService = new WatcherProcessService();
            AppVersionProvider appVersionProvider = new AppVersionProvider();
            UpdateSettingsProvider updateSettingsProvider = new UpdateSettingsProvider();
            GitHubUpdateService updateService = new GitHubUpdateService(updateSettingsProvider, appVersionProvider);
            InstallerLauncherService installerLauncherService = new InstallerLauncherService();
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
                    currentUser = RunCommandConsole(
                        currentUser,
                        noteService,
                        userAdminService,
                        auditLogService,
                        watcherReportService,
                        watcherProcessService,
                        appVersionProvider,
                        updateService,
                        installerLauncherService);
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
            UserAdminService userAdminService,
            AuditLogService auditLogService,
            WatcherReportService watcherReportService,
            WatcherProcessService watcherProcessService,
            AppVersionProvider appVersionProvider,
            GitHubUpdateService updateService,
            InstallerLauncherService installerLauncherService)
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
                else if (command.Equals("version", StringComparison.OrdinalIgnoreCase))
                {
                    PrintVersion(appVersionProvider);
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
                    RunAdminCommand(currentUser, userAdminService, noteService, auditLogService, command);
                }
                else if (IsWatcherCommand(command))
                {
                    RunWatcherCommand(currentUser, watcherReportService, watcherProcessService, command);
                }
                else if (IsUpdateCommand(command))
                {
                    RunUpdateCommand(currentUser, updateService, installerLauncherService, command);
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
            Console.WriteLine("version                             показать текущую версию");
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
                Console.WriteLine("listUserNotes <логин>               показать заметки пользователя");
                Console.WriteLine("showLogs <количество>               показать последние записи журнала");
                Console.WriteLine("blockUser <логин>                   заблокировать пользователя");
                Console.WriteLine("unblockUser <логин>                 разблокировать пользователя");
                Console.WriteLine("deleteUser <логин>                  удалить пользователя");
                Console.WriteLine("updateCheck                         проверить обновление на GitHub");
                Console.WriteLine("updateInstall                       скачать и установить обновление");
                Console.WriteLine("Роли для createUser: user, admin, analyst");
            }

            if (CanViewWatcher(user))
            {
                Console.WriteLine("startWatcher [секунды]              запустить watcher");
                Console.WriteLine("listDevices                         показать устройства watcher-а");
                Console.WriteLine("showMetrics <deviceId>              показать последние метрики устройства");
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
                   command.Equals("listUserNotes", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("listUserNotes ", StringComparison.OrdinalIgnoreCase) ||
                   command.Equals("showLogs", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("showLogs ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("blockUser ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("unblockUser ", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("deleteUser ", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWatcherCommand(string command)
        {
            return command.Equals("listDevices", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("showMetrics ", StringComparison.OrdinalIgnoreCase) ||
                   command.Equals("startWatcher", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("startWatcher ", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUpdateCommand(string command)
        {
            return command.Equals("updateCheck", StringComparison.OrdinalIgnoreCase) ||
                   command.Equals("updateInstall", StringComparison.OrdinalIgnoreCase);
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

        private static void RunAdminCommand(
            AppUser currentUser,
            UserAdminService userAdminService,
            NoteService noteService,
            AuditLogService auditLogService,
            string command)
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
            else if (command.Equals("listUserNotes", StringComparison.OrdinalIgnoreCase) ||
                     command.StartsWith("listUserNotes ", StringComparison.OrdinalIgnoreCase))
            {
                PrintUserNotes(currentUser, noteService, command);
            }
            else if (command.Equals("showLogs", StringComparison.OrdinalIgnoreCase) ||
                     command.StartsWith("showLogs ", StringComparison.OrdinalIgnoreCase))
            {
                PrintAuditLogs(currentUser, auditLogService, command);
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

        private static void RunWatcherCommand(
            AppUser currentUser,
            WatcherReportService watcherReportService,
            WatcherProcessService watcherProcessService,
            string command)
        {
            if (!CanViewWatcher(currentUser))
            {
                Console.WriteLine("Команды watcher-а доступны только администратору и аналитику.");
                return;
            }

            if (command.Equals("listDevices", StringComparison.OrdinalIgnoreCase))
            {
                PrintWatcherDevices(currentUser, watcherReportService);
            }
            else if (command.StartsWith("showMetrics ", StringComparison.OrdinalIgnoreCase))
            {
                PrintDeviceMetrics(currentUser, watcherReportService, command);
            }
            else if (command.Equals("startWatcher", StringComparison.OrdinalIgnoreCase) ||
                     command.StartsWith("startWatcher ", StringComparison.OrdinalIgnoreCase))
            {
                StartWatcher(watcherProcessService, command);
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

        private static void PrintUserNotes(AppUser currentUser, NoteService noteService, string command)
        {
            string username = command.Length > "listUserNotes".Length
                ? command.Substring("listUserNotes".Length).Trim()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Формат команды: listUserNotes <логин>");
                return;
            }

            try
            {
                List<NoteRecord> notes = noteService.GetUserNotes(currentUser, username);

                if (notes.Count == 0)
                {
                    Console.WriteLine("У пользователя " + username + " пока нет заметок.");
                    return;
                }

                foreach (NoteRecord note in notes)
                {
                    Console.WriteLine(note.Id + " | " + note.OwnerUsername + " | " + note.CreatedAt.ToString("yyyy-MM-dd HH:mm") + " | " + note.Content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения заметок пользователя: " + ex.Message);
            }
        }

        private static void PrintAuditLogs(AppUser currentUser, AuditLogService auditLogService, string command)
        {
            string limitText = command.Length > "showLogs".Length
                ? command.Substring("showLogs".Length).Trim()
                : string.Empty;

            if (!int.TryParse(limitText, out int limit) || limit <= 0)
            {
                Console.WriteLine("Формат команды: showLogs <количество>");
                return;
            }

            try
            {
                List<AuditEventRecord> logs = auditLogService.GetLastLogs(currentUser, limit);

                if (logs.Count == 0)
                {
                    Console.WriteLine("Журнал действий пока пуст.");
                    return;
                }

                foreach (AuditEventRecord log in logs)
                {
                    string accountName = string.IsNullOrWhiteSpace(log.AccountName) ? "system" : log.AccountName;
                    string objectName = string.IsNullOrWhiteSpace(log.ObjectName) ? "-" : log.ObjectName;

                    Console.WriteLine(log.Id + " | " +
                                      log.EventTime.ToString("yyyy-MM-dd HH:mm:ss") + " | " +
                                      accountName + " | " +
                                      log.ActionCode + " | " +
                                      objectName + " | " +
                                      log.Details);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения журнала действий: " + ex.Message);
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

        private static void PrintWatcherDevices(AppUser currentUser, WatcherReportService watcherReportService)
        {
            try
            {
                List<WatcherDeviceRecord> devices = watcherReportService.GetDevices(currentUser);

                if (devices.Count == 0)
                {
                    Console.WriteLine("Устройства watcher-а пока не найдены.");
                    return;
                }

                foreach (WatcherDeviceRecord device in devices)
                {
                    string lastContact = device.LastContactAt.HasValue
                        ? device.LastContactAt.Value.ToString("yyyy-MM-dd HH:mm")
                        : "нет данных";

                    Console.WriteLine(device.Id + " | " + device.DeviceUid + " | последний контакт: " + lastContact);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения устройств watcher-а: " + ex.Message);
            }
        }

        private static void PrintDeviceMetrics(
            AppUser currentUser,
            WatcherReportService watcherReportService,
            string command)
        {
            string idText = command.Substring("showMetrics ".Length).Trim();

            if (!int.TryParse(idText, out int deviceId))
            {
                Console.WriteLine("Формат команды: showMetrics <deviceId>");
                return;
            }

            try
            {
                List<DeviceMetricRecord> metrics = watcherReportService.GetMetrics(currentUser, deviceId);

                if (metrics.Count == 0)
                {
                    Console.WriteLine("Метрики для устройства не найдены.");
                    return;
                }

                foreach (DeviceMetricRecord metric in metrics)
                {
                    Console.WriteLine(metric.CapturedAt.ToString("yyyy-MM-dd HH:mm:ss") +
                                      " | CPU: " + metric.CpuLoad.ToString("0.00") + "%" +
                                      " | RAM: " + metric.RamLoad.ToString("0.00") + "%" +
                                      " | HDD: " + metric.DiskLoad.ToString("0.00") + "%");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения метрик watcher-а: " + ex.Message);
            }
        }

        private static void StartWatcher(WatcherProcessService watcherProcessService, string command)
        {
            int? intervalSeconds = null;

            if (command.StartsWith("startWatcher ", StringComparison.OrdinalIgnoreCase))
            {
                string intervalText = command.Substring("startWatcher ".Length).Trim();

                if (!int.TryParse(intervalText, out int parsedInterval) || parsedInterval <= 0)
                {
                    Console.WriteLine("Формат команды: startWatcher [секунды]");
                    return;
                }

                intervalSeconds = parsedInterval;
            }

            try
            {
                string message = watcherProcessService.StartWatcher(intervalSeconds);
                Console.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка запуска watcher-а: " + ex.Message);
            }
        }

        private static void PrintVersion(AppVersionProvider appVersionProvider)
        {
            Console.WriteLine("Текущая версия NotesApp: " + appVersionProvider.GetCurrentVersionText());
        }

        private static void RunUpdateCommand(
            AppUser currentUser,
            GitHubUpdateService updateService,
            InstallerLauncherService installerLauncherService,
            string command)
        {
            if (!IsAdmin(currentUser))
            {
                Console.WriteLine("Команды обновления доступны только администратору.");
                return;
            }

            if (command.Equals("updateCheck", StringComparison.OrdinalIgnoreCase))
            {
                PrintUpdateCheck(updateService);
            }
            else if (command.Equals("updateInstall", StringComparison.OrdinalIgnoreCase))
            {
                InstallUpdate(updateService, installerLauncherService);
            }
        }

        private static void PrintUpdateCheck(GitHubUpdateService updateService)
        {
            try
            {
                UpdateInfo updateInfo = updateService.CheckForUpdate();

                Console.WriteLine("Текущая версия: " + updateInfo.CurrentVersion);
                Console.WriteLine("Последний релиз: " + updateInfo.LatestVersion);

                if (!string.IsNullOrWhiteSpace(updateInfo.ReleaseUrl))
                {
                    Console.WriteLine("Ссылка: " + updateInfo.ReleaseUrl);
                }

                Console.WriteLine(updateInfo.HasUpdate
                    ? "Доступно обновление."
                    : "Установлена актуальная версия.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка проверки обновления: " + ex.Message);
            }
        }

        private static void InstallUpdate(
            GitHubUpdateService updateService,
            InstallerLauncherService installerLauncherService)
        {
            try
            {
                UpdateInfo updateInfo = updateService.CheckForUpdate();

                if (!updateInfo.HasUpdate)
                {
                    Console.WriteLine("Установлена актуальная версия.");
                    return;
                }

                Console.WriteLine("Скачивание обновления " + updateInfo.LatestVersion + "...");
                string archivePath = updateService.DownloadArchive(updateInfo);

                Console.WriteLine("Запуск Installer...");
                installerLauncherService.Launch(archivePath);

                Console.WriteLine("NotesApp будет закрыт для установки обновления.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка установки обновления: " + ex.Message);
            }
        }

        private static bool CanUseNotes(AppUser user)
        {
            return user.RoleCode == "user" || user.RoleCode == "admin";
        }

        private static bool IsAdmin(AppUser user)
        {
            return user.RoleCode == "admin";
        }

        private static bool CanViewWatcher(AppUser user)
        {
            return user.RoleCode == "admin" || user.RoleCode == "analyst";
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
