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