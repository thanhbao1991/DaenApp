using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface INguyenLieuService
{
    Task<List<NguyenLieuDto>> GetAllAsync();
    Task<NguyenLieuDto?> GetByIdAsync(Guid id);
    Task<Result<NguyenLieuDto>> CreateAsync(NguyenLieuDto dto);
    Task<Result<NguyenLieuDto>> UpdateAsync(Guid id, NguyenLieuDto dto);
    Task<Result<NguyenLieuDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuDto>> RestoreAsync(Guid id);
    Task<List<NguyenLieuDto>> GetUpdatedSince(DateTime lastSync);
}
