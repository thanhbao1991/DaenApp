using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IKhachHangService
{
    Task<Result<KhachHangDto>> RestoreAsync(Guid id);
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto);
    Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result<KhachHangDto>> UpdateSingleAsync(Guid id, KhachHangDto dto);
    Task<Result<KhachHangDto>> DeleteAsync(Guid id);

    // Đồng bộ ngoại tuyến
    Task<List<KhachHangDto>> GetUpdatedSince(DateTime lastSync);

    // ✅ MỚI — Search theo tên/SDT/địa chỉ (accent-insensitive), giới hạn trả về
    Task<List<KhachHangDto>> SearchAsync(string q, int take = 30);
}