using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface INguyenLieuApi
{
    Task<Result<List<NguyenLieuDto>>> GetAllAsync();
    Task<Result<NguyenLieuDto>> GetByIdAsync(Guid id);
    Task<Result<List<NguyenLieuDto>>> GetUpdatedSince(DateTime since);
    Task<Result<NguyenLieuDto>> CreateAsync(NguyenLieuDto dto);
    Task<Result<NguyenLieuDto>> UpdateAsync(Guid id, NguyenLieuDto dto);
    Task<Result<NguyenLieuDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuDto>> RestoreAsync(Guid id);
}
