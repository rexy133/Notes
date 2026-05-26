namespace NotesApp.Models
{
    /// <summary>
    /// Настройки проверки обновлений.
    /// </summary>
    public class UpdateSettings
    {
        public string Owner { get; set; }

        public string Repo { get; set; }

        public string AssetExtension { get; set; }

        public int HttpTimeoutSeconds { get; set; }
    }
}
