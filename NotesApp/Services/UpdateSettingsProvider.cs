using System;
using System.IO;
using NotesApp.Models;

namespace NotesApp.Services
{
    /// <summary>
    /// Читает настройки обновлений из файла notes.yml.
    /// </summary>
    public class UpdateSettingsProvider
    {
        private const string _settingsFileName = "notes.yml";

        public UpdateSettings Load()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settingsFileName);

            if (!File.Exists(settingsPath))
            {
                throw new FileNotFoundException("Файл настроек обновлений notes.yml не найден.", settingsPath);
            }

            UpdateSettings settings = new UpdateSettings
            {
                AssetExtension = ".zip",
                HttpTimeoutSeconds = 15
            };

            bool inUpdatesSection = false;
            string[] lines = File.ReadAllLines(settingsPath);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.Length == 0 || trimmedLine.StartsWith("#"))
                {
                    continue;
                }

                if (trimmedLine == "updates:")
                {
                    inUpdatesSection = true;
                    continue;
                }

                if (!inUpdatesSection)
                {
                    continue;
                }

                int separatorIndex = trimmedLine.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                string key = trimmedLine.Substring(0, separatorIndex).Trim();
                string value = trimmedLine.Substring(separatorIndex + 1).Trim().Trim('"', '\'');

                ApplyValue(settings, key, value);
            }

            Validate(settings);
            return settings;
        }

        private static void ApplyValue(UpdateSettings settings, string key, string value)
        {
            if (key.Equals("owner", StringComparison.OrdinalIgnoreCase))
            {
                settings.Owner = value;
            }
            else if (key.Equals("repo", StringComparison.OrdinalIgnoreCase))
            {
                settings.Repo = value;
            }
            else if (key.Equals("assetExtension", StringComparison.OrdinalIgnoreCase))
            {
                settings.AssetExtension = value;
            }
            else if (key.Equals("httpTimeoutSeconds", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value, out int seconds) && seconds > 0)
                {
                    settings.HttpTimeoutSeconds = seconds;
                }
            }
        }

        private static void Validate(UpdateSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Owner))
            {
                throw new InvalidOperationException("В notes.yml не указан updates.owner.");
            }

            if (string.IsNullOrWhiteSpace(settings.Repo))
            {
                throw new InvalidOperationException("В notes.yml не указан updates.repo.");
            }

            if (string.IsNullOrWhiteSpace(settings.AssetExtension))
            {
                settings.AssetExtension = ".zip";
            }
        }
    }
}
