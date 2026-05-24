namespace NotesApp.Models
{
    /// <summary>
    /// Пользователь приложения, прочитанный из базы данных.
    /// </summary>
    public class AppUser
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string RoleCode { get; set; }

        public string RoleTitle { get; set; }

        public bool Blocked { get; set; }
    }
}
