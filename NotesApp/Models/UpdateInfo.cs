namespace NotesApp.Models
{
    /// <summary>
    /// Информация о найденном релизе приложения.
    /// </summary>
    public class UpdateInfo
    {
        public string CurrentVersion { get; set; }

        public string LatestVersion { get; set; }

        public string ReleaseName { get; set; }

        public string ReleaseUrl { get; set; }

        public string ArchiveDownloadUrl { get; set; }

        public bool HasUpdate { get; set; }
    }
}
