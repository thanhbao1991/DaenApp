using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;

namespace TraSuaApp.WpfClient.Services
{
    public class DoanhThuApi : BaseApi
    {
        private const string BASE_URL = "/api/DoanhThu";
        public DoanhThuApi() : base(TuDien._tableFriendlyNames["DoanhThu"]) { }
        public async Task<Result<List<DoanhThuNamItemDto>>> GetDoanhThuNam(int nam)
        {
            return await GetAsync<List<DoanhThuNamItemDto>>
                ($"api/doanhthu/nam?nam={nam}");
        }
        public async Task<Result<List<DoanhThuThangItemDto>>> GetDoanhThuThang(int thang, int nam)
        {
            return await GetAsync<List<DoanhThuThangItemDto>>
                ($"api/doanhthu/thang?thang={thang}&nam={nam}");
        }

    }
}