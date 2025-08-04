namespace TraSuaApp.Shared.Config
{
    public static class ApiConfig
    {
        public static string BaseUrl => "https://localhost:5001";
        public static string LoginEndpoint => $"{BaseUrl}/api/auth/login";
    }
}
