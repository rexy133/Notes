namespace NotesApp.Security
{
    /// <summary>
    /// Создает и проверяет хэши паролей через BCrypt.
    /// </summary>
    public static class PasswordHasher
    {
        /// <summary>
        /// Создает хэш пароля. Соль хранится внутри BCrypt-хэша.
        /// </summary>
        /// <param name="password">Пароль в открытом виде.</param>
        public static string CreateHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return string.Empty;
            }

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Проверяет введенный пароль по сохраненному BCrypt-хэшу.
        /// </summary>
        /// <param name="password">Пароль в открытом виде.</param>
        /// <param name="storedHash">Сохраненный BCrypt-хэш пароля.</param>
        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
            catch (BCrypt.Net.HashInformationException)
            {
                return false;
            }
        }
    }
}
