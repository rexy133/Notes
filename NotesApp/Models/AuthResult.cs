namespace NotesApp.Models
{
    /// <summary>
    /// Результат регистрации или входа пользователя.
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public AppUser User { get; set; }
    }
}
