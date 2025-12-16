using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface INguyenLieuBanHangService
{
    Task<List<NguyenLieuBanHangDto>> GetAllAsync();
    Task<NguyenLieuBanHangDto?> GetByIdAsync(Guid id);
    Task<Result<NguyenLieuBanHangDto>> CreateAsync(NguyenLieuBanHangDto dto);
    Task<Result<NguyenLieuBanHangDto>> UpdateAsync(Guid id, NguyenLieuBanHangDto dto);
    Task<Result<NguyenLieuBanHangDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuBanHangDto>> RestoreAsync(Guid id);
    Task<List<NguyenLieuBanHangDto>> GetUpdatedSince(DateTime lastSync);
}
