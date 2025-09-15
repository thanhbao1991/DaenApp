namespace TraSuaApp.Shared.Config
{
    public static class Config
    {
        static Config()
        {
            // 🟟 Danh sách máy dev (có thể thêm nhiều tên máy tại đây)
            var devMachines = new[] { "ADMIN", "YOUR-DEV-PC" };
            var api = "http://api.denncoffee.uk";

            if (devMachines.Contains(Environment.MachineName, StringComparer.OrdinalIgnoreCase))
            {
                ApiBaseUrl = "http://localhost:8080";
                ConnectionString =
                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
            }
            else
            {
                ApiBaseUrl = api;
                ConnectionString =
                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
            }

            // Luôn lấy SignalRHubUrl từ ApiBaseUrl để đồng bộ
            SignalRHubUrl = $"{api}/hub/entity";
        }

        public static string ApiBaseUrl { get; }
        public static string ConnectionString { get; }
        public static string SignalRHubUrl { get; } // 🟟 thêm property cho SignalR
        public static string apiChatGptKey = "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";
    }
}