namespace TraSuaApp.Shared.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Băm mật khẩu với BCrypt (tự động sinh salt).
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Xác minh mật khẩu người dùng với mật khẩu đã băm.
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}