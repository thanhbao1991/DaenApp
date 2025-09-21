using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services
{
    public class ThongKeApi : BaseApi
    {
        private const string BASE_URL = "/api/ThongKe";
        public ThongKeApi() : base(TuDien._tableFriendlyNames["ThongKe"]) { }

        // Gửi "yyyy-MM-dd" (date-only) để bind vào DateOnly trên server
        public Task<Result<ThongKeNgayDto>> GetByDateAsync(DateTime date)
            => GetAsync<ThongKeNgayDto>($"{BASE_URL}/ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}");


    }
}