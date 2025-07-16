using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IKhachHangService
{
    Task<Result<KhachHangDto>> RestoreAsync(Guid id);
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto);
    Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result<KhachHangDto>> DeleteAsync(Guid id);

    // 🟟 API đồng bộ
    Task<List<KhachHangDto>> GetUpdatedSince(DateTime lastSync);
}