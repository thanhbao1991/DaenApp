using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Services
{
    public class DashboardApi
    {
        public async Task<Result<List<SanPhamXepHangDto>>> GetXepHangSanPham(int year)
        {
            try
            {
                var result = await ApiClient.Get<Result<List<SanPhamXepHangDto>>>($"/api/dashboard/xephang-sanpham?year={year}");
                return result ?? Result<List<SanPhamXepHangDto>>.Failure("Không nhận được dữ liệu từ server.");
            }
            catch (Exception ex)
            {
                return Result<List<SanPhamXepHangDto>>.Failure($"Lỗi khi tải dữ liệu xếp hạng sản phẩm: {ex.Message}");
            }
        }

        public async Task<Result<List<KhachHangXepHangDto>>> GetXepHangKhachHang(int year)
        {
            try
            {
                var result = await ApiClient.Get<Result<List<KhachHangXepHangDto>>>($"/api/dashboard/xephang-khachhang?year={year}");
                return result ?? Result<List<KhachHangXepHangDto>>.Failure("Không nhận được dữ liệu từ server.");
            }
            catch (Exception ex)
            {
                return Result<List<KhachHangXepHangDto>>.Failure($"Lỗi khi tải dữ liệu xếp hạng khách hàng: {ex.Message}");
            }
        }

        public async Task<Result<List<ChiTieuHangNgayDto>>> GetChiTieuByNguyenLieuId(int offset, Guid? nguyenLieuId = null)
        {
            try
            {
                var uri = $"/api/dashboard/chitieubynguyenlieuid?offset={offset}";
                if (nguyenLieuId != null && nguyenLieuId != Guid.Empty)
                    uri += $"&nguyenLieuId={nguyenLieuId}";

                var result = await ApiClient.Get<Result<List<ChiTieuHangNgayDto>>>(uri);
                return result ?? Result<List<ChiTieuHangNgayDto>>.Failure("Không nhận được dữ liệu từ server.");
            }
            catch (Exception ex)
            {
                return Result<List<ChiTieuHangNgayDto>>.Failure($"Lỗi khi tải dữ liệu chi tiêu: {ex.Message}");
            }
        }

        public async Task<Result<List<VoucherChiTraDto>>> GetVoucher(int offset, Guid? voucherId = null)
        {
            try
            {
                var uri = $"/api/dashboard/voucher?offset={offset}";
                if (voucherId != null && voucherId != Guid.Empty)
                    uri += $"&voucherId={voucherId}";

                var result = await ApiClient.Get<Result<List<VoucherChiTraDto>>>(uri);
                return result ?? Result<List<VoucherChiTraDto>>.Failure("Không nhận được dữ liệu từ server.");
            }
            catch (Exception ex)
            {
                return Result<List<VoucherChiTraDto>>.Failure($"Lỗi khi tải dữ liệu voucher: {ex.Message}");
            }
        }
    }
}