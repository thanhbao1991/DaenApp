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

        public Task<Result<ThongKeChiTieuDto>> GetByDateAsync(DateTime date)
      => GetAsync<ThongKeChiTieuDto>($"{BASE_URL}/chi-tieu-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}");

        public Task<Result<ThongKeCongNoDto>> GetCongNoByDateAsync(DateTime date)
         => GetAsync<ThongKeCongNoDto>($"{BASE_URL}/cong-no-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}");

        public Task<Result<ThongKeThanhToanDto>> GetThanhToanByDateAsync(DateTime date)
         => GetAsync<ThongKeThanhToanDto>(
             $"{BASE_URL}/thanh-toan-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}");

        public Task<Result<ThongKeDoanhThuNgayDto>> GetDoanhThuNgayAsync(DateTime date)
        {
            var url =
                $"{BASE_URL}/doanh-thu-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}";

            return GetAsync<ThongKeDoanhThuNgayDto>(url);
        }

        public Task<Result<ThongKeTraNoNgayDto>> GetTraNoNgayAsync(DateTime date)
        {
            var url =
                $"{BASE_URL}/tra-no-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}";

            return GetAsync<ThongKeTraNoNgayDto>(url);
        }
    }
}