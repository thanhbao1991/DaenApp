using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IChiTieuHangNgayService
{
    Task<List<ChiTieuHangNgayDto>> GetAllAsync();
    Task<ChiTieuHangNgayDto?> GetByIdAsync(Guid id);
    Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto);
    Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto);
    Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id);
    Task<Result<ChiTieuHangNgayDto>> RestoreAsync(Guid id);
    Task<List<ChiTieuHangNgayDto>> GetUpdatedSince(DateTime lastSync);
}
