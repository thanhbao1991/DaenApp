using System.Net.Http;
using System.Net.Http.Json;
using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services
{
    // =========================================================
    // BASE API (GENERIC)
    // =========================================================
    public class BaseApi<TDto>
    {
        protected readonly string _baseUrl;
        protected readonly string _friendlyName;

        public BaseApi(string baseUrl, string friendlyName)
        {
            _baseUrl = baseUrl;
            _friendlyName = friendlyName;
        }

        // =========================
        // HANDLE RESPONSE
        // =========================
        protected async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    return Result<T>.Failure($"400 - {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return Result<T>.Failure("Hết phiên đăng nhập");

                return Result<T>.Failure(
                    $"API {(int)response.StatusCode} - {response.ReasonPhrase}\n{errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<Result<T>>()
                   ?? Result<T>.Failure($"Không đọc được {_friendlyName}");
        }

        // =========================
        // BASE METHODS
        // =========================
        protected async Task<Result<T>> GetAsync<T>(string url, CancellationToken ct = default)
            => await HandleResponseAsync<T>(await ApiClient.GetAsync(url, true, ct));

        protected async Task<Result<T>> PostAsync<T>(string url, object? dto, CancellationToken ct = default)
            => await HandleResponseAsync<T>(await ApiClient.PostAsync(url, dto, true, ct));

        protected async Task<Result<T>> PutAsync<T>(string url, object? dto, CancellationToken ct = default)
            => await HandleResponseAsync<T>(await ApiClient.PutAsync(url, dto, true, ct));

        protected async Task<Result<T>> DeleteAsync<T>(string url, CancellationToken ct = default)
            => await HandleResponseAsync<T>(await ApiClient.DeleteAsync(url, true, ct));

        // =========================
        // CRUD CHUẨN
        // =========================
        public Task<Result<List<TDto>>> GetAllAsync(CancellationToken ct = default)
            => GetAsync<List<TDto>>(_baseUrl, ct);

        public Task<Result<TDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
            => GetAsync<TDto>($"{_baseUrl}/{id}", ct);

        public Task<Result<List<TDto>>> GetUpdatedSince(DateTime since, CancellationToken ct = default)
            => GetAsync<List<TDto>>(
                $"{_baseUrl}/sync?lastSync={Uri.EscapeDataString(since.ToUniversalTime().ToString("o"))}",
                ct);

        public Task<Result<TDto>> CreateAsync(TDto dto, CancellationToken ct = default)
            => PostAsync<TDto>(_baseUrl, dto, ct);

        public Task<Result<TDto>> UpdateAsync(Guid id, TDto dto, CancellationToken ct = default)
            => PutAsync<TDto>($"{_baseUrl}/{id}", dto, ct);

        public Task<Result<TDto>> DeleteAsync(Guid id, CancellationToken ct = default)
            => DeleteAsync<TDto>($"{_baseUrl}/{id}", ct);
    }

    // =========================================================
    // API CONTAINER (THAY THẾ TOÀN BỘ XxxApi)
    // =========================================================
    public static class Apis
    {
        public static BaseApi<HoaDonDto> HoaDon =>
            new("/api/HoaDon", TuDien._tableFriendlyNames["HoaDon"]);

        public static BaseApi<KhachHangDto> KhachHang =>
            new("/api/KhachHang", TuDien._tableFriendlyNames["KhachHang"]);

        public static BaseApi<VoucherDto> Voucher =>
            new("/api/Voucher", TuDien._tableFriendlyNames["Voucher"]);

        public static BaseApi<SanPhamDto> SanPham =>
            new("/api/SanPham", TuDien._tableFriendlyNames["SanPham"]);

        public static BaseApi<ToppingDto> Topping =>
            new("/api/Topping", TuDien._tableFriendlyNames["Topping"]);

        public static BaseApi<NhomSanPhamDto> NhomSanPham =>
            new("/api/NhomSanPham", TuDien._tableFriendlyNames["NhomSanPham"]);

        public static BaseApi<NguyenLieuDto> NguyenLieu =>
            new("/api/NguyenLieu", TuDien._tableFriendlyNames["NguyenLieu"]);

        public static BaseApi<NguyenLieuBanHangDto> NguyenLieuBanHang =>
            new("/api/NguyenLieuBanHang", TuDien._tableFriendlyNames["NguyenLieuBanHang"]);


        public static BaseApi<PhuongThucThanhToanDto> PhuongThucThanhToan =>
            new("/api/PhuongThucThanhToan", TuDien._tableFriendlyNames["PhuongThucThanhToan"]);


        public static BaseApi<TaiKhoanDto> TaiKhoan =>
            new("/api/TaiKhoan", TuDien._tableFriendlyNames["TaiKhoan"]);

        public static BaseApi<TuDienTraCuuDto> TuDienTraCuu =>
            new("/api/TuDienTraCuu", TuDien._tableFriendlyNames["TuDienTraCuu"]);

        public static BaseApi<ChiTieuHangNgayDto> ChiTieuHangNgay =>
            new("/api/ChiTieuHangNgay", TuDien._tableFriendlyNames["ChiTieuHangNgay"]);

        public static BaseApi<CongThucDto> CongThuc =>
            new("/api/CongThuc", TuDien._tableFriendlyNames["CongThuc"]);

        public static BaseApi<CongViecNoiBoDto> CongViecNoiBo =>
            new("/api/CongViecNoiBo", TuDien._tableFriendlyNames["CongViecNoiBo"]);

        public static BaseApi<SuDungNguyenLieuDto> SuDungNguyenLieu =>
            new("/api/SuDungNguyenLieu", TuDien._tableFriendlyNames["SuDungNguyenLieu"]);

        public static BaseApi<KhachHangGiaBanDto> KhachHangGiaBan =>
          new("/api/KhachHangGiaBan", TuDien._tableFriendlyNames["KhachHangGiaBan"]);
    }
}