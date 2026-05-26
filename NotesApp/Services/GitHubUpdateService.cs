using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using NotesApp.Models;

namespace NotesApp.Services
{
    /// <summary>
    /// Проверяет GitHub Releases и скачивает архив обновления.
    /// </summary>
    public class GitHubUpdateService
    {
        private readonly UpdateSettingsProvider _settingsProvider;
        private readonly AppVersionProvider _versionProvider;

        public GitHubUpdateService(UpdateSettingsProvider settingsProvider, AppVersionProvider versionProvider)
        {
            _settingsProvider = settingsProvider;
            _versionProvider = versionProvider;
        }

        public UpdateInfo CheckForUpdate()
        {
            UpdateSettings settings = _settingsProvider.Load();
            Version currentVersion = _versionProvider.GetCurrentVersion();
            string releaseJson = LoadLatestReleaseJson(settings);

            using (JsonDocument document = JsonDocument.Parse(releaseJson))
            {
                JsonElement root = document.RootElement;
                string tagName = GetString(root, "tag_name");
                Version latestVersion = ParseReleaseVersion(tagName);
                string releaseName = GetString(root, "name");
                string releaseUrl = GetString(root, "html_url");
                string archiveDownloadUrl = FindArchiveDownloadUrl(root, settings.AssetExtension);

                return new UpdateInfo
                {
                    CurrentVersion = _versionProvider.GetCurrentVersionText(),
                    LatestVersion = FormatVersion(latestVersion),
                    ReleaseName = string.IsNullOrWhiteSpace(releaseName) ? tagName : releaseName,
                    ReleaseUrl = releaseUrl,
                    ArchiveDownloadUrl = archiveDownloadUrl,
                    HasUpdate = latestVersion.CompareTo(currentVersion) > 0
                };
            }
        }

        public string DownloadArchive(UpdateInfo updateInfo)
        {
            if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.ArchiveDownloadUrl))
            {
                throw new InvalidOperationException("В последнем релизе не найден zip-архив обновления.");
            }

            UpdateSettings settings = _settingsProvider.Load();
            string downloadFolder = Path.Combine(Path.GetTempPath(), "NotesUpdates");
            Directory.CreateDirectory(downloadFolder);

            string archivePath = Path.Combine(downloadFolder, "NotesUpdate_" + updateInfo.LatestVersion + ".zip");

            using (HttpClient client = CreateHttpClient(settings))
            {
                byte[] archiveBytes = client.GetByteArrayAsync(updateInfo.ArchiveDownloadUrl).GetAwaiter().GetResult();
                File.WriteAllBytes(archivePath, archiveBytes);
            }

            return archivePath;
        }

        private static string LoadLatestReleaseJson(UpdateSettings settings)
        {
            string url = "https://api.github.com/repos/" + settings.Owner + "/" + settings.Repo + "/releases/latest";

            using (HttpClient client = CreateHttpClient(settings))
            using (HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult())
            {
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new InvalidOperationException("В репозитории " + settings.Owner + "/" + settings.Repo + " пока нет GitHub Release.");
                    }

                    throw new InvalidOperationException("Не удалось получить последний GitHub Release. Код ответа: " + (int)response.StatusCode + ".");
                }

                return content;
            }
        }

        private static HttpClient CreateHttpClient(UpdateSettings settings)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(settings.HttpTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NotesApp");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            return client;
        }

        private static string FindArchiveDownloadUrl(JsonElement root, string assetExtension)
        {
            if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string name = GetString(asset, "name");

                if (name != null && name.EndsWith(assetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return GetString(asset, "browser_download_url");
                }
            }

            return null;
        }

        private static Version ParseReleaseVersion(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName))
            {
                throw new InvalidOperationException("В GitHub Release не указан tag_name.");
            }

            string versionText = tagName.Trim();

            if (versionText.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                versionText = versionText.Substring(1);
            }

            int suffixIndex = versionText.IndexOf('-');
            if (suffixIndex > 0)
            {
                versionText = versionText.Substring(0, suffixIndex);
            }

            if (!Version.TryParse(versionText, out Version version))
            {
                throw new InvalidOperationException("Тег релиза должен быть в формате v1.0.1.");
            }

            return AppVersionProvider.Normalize(version);
        }

        private static string FormatVersion(Version version)
        {
            return version.Major + "." + version.Minor + "." + version.Build;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return value.GetString();
        }
    }
}
