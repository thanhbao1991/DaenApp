using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.WpfClient.Services
{
    public class ThongKeApi : BaseApi<object> // 👈 dùng object vì không có DTO chung
    {
        private const string BASE_URL = "/api/ThongKe";

        public ThongKeApi()
            : base(BASE_URL, TuDien._tableFriendlyNames["ThongKe"])
        {
        }

        // =========================
        // GET
        // =========================
        public Task<Result<ThongKeChiTieuDto>> GetByDateAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeChiTieuDto>(
                $"{BASE_URL}/chi-tieu-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);

        public Task<Result<ThongKeCongNoDto>> GetCongNoByDateAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeCongNoDto>(
                $"{BASE_URL}/cong-no-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);

        public Task<Result<ThongKeThanhToanDto>> GetThanhToanByDateAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeThanhToanDto>(
                $"{BASE_URL}/thanh-toan-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);

        public Task<Result<ThongKeDoanhThuNgayDto>> GetDoanhThuNgayAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeDoanhThuNgayDto>(
                $"{BASE_URL}/doanh-thu-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);

        public Task<Result<ThongKeTraNoNgayDto>> GetTraNoNgayAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeTraNoNgayDto>(
                $"{BASE_URL}/tra-no-ngay?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);

        public Task<Result<ThongKeDonChuaThanhToanDto>> GetDonChuaThanhToanAsync(DateTime date, CancellationToken ct = default)
            => GetAsync<ThongKeDonChuaThanhToanDto>(
                $"{BASE_URL}/don-chua-thanh-toan?ngay={date.Day}&thang={date.Month}&nam={date.Year}",
                ct);
    }
}