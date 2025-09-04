//namespace TraSuaApp.Shared.Config
//{
//    public static class Config
//    {
//        static Config()
//        {
//            // Danh sách máy dev (có thể thêm nhiều tên máy)
//            var devMachines = new[] { "ADMIN", "" };

//            if (devMachines.Contains(Environment.MachineName, StringComparer.OrdinalIgnoreCase))
//            {
//                ApiBaseUrl = "http://localhost:5093";
//                ConnectionString =
//                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
//            }
//            else
//            {
//                ApiBaseUrl = "http://api.denncoffee.uk";
//                ConnectionString =
//                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
//            }
//        }

//        public static string ApiBaseUrl { get; }
//        public static string ConnectionString { get; }
//    }
//}
namespace TraSuaApp.Shared.Config
{
    public static class Config
    {
        static Config()
        {
            // 🟟 Danh sách máy dev (có thể thêm nhiều tên máy tại đây)
            var devMachines = new[] { "ADMIN", "YOUR-DEV-PC" };

            if (devMachines.Contains(Environment.MachineName, StringComparer.OrdinalIgnoreCase))
            {
                ApiBaseUrl = "http://localhost:5093";
                ConnectionString =
                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
            }
            else
            {
                ApiBaseUrl = "http://api.denncoffee.uk";
                ConnectionString =
                    "Server=localhost;Database=TraSuaAppDb;User Id=appuser;Password=StrongPassword@123;TrustServerCertificate=True;";
            }

            // Luôn lấy SignalRHubUrl từ ApiBaseUrl để đồng bộ
            SignalRHubUrl = $"{ApiBaseUrl}/hub/entity";
        }

        public static string ApiBaseUrl { get; }
        public static string ConnectionString { get; }
        public static string SignalRHubUrl { get; } // 🟟 thêm property cho SignalR
    }
}