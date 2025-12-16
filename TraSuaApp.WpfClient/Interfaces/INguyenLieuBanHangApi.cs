using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface INguyenLieuBanHangApi
{
    Task<Result<List<NguyenLieuBanHangDto>>> GetAllAsync();
    Task<Result<NguyenLieuBanHangDto>> GetByIdAsync(Guid id);
    Task<Result<List<NguyenLieuBanHangDto>>> GetUpdatedSince(DateTime since);
    Task<Result<NguyenLieuBanHangDto>> CreateAsync(NguyenLieuBanHangDto dto);
    Task<Result<NguyenLieuBanHangDto>> UpdateAsync(Guid id, NguyenLieuBanHangDto dto);
    Task<Result<NguyenLieuBanHangDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuBanHangDto>> RestoreAsync(Guid id);
}
