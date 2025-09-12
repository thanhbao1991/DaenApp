using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IChiTieuHangNgayApi
{
    Task<Result<List<ChiTieuHangNgayDto>>> GetAllAsync();
    Task<Result<ChiTieuHangNgayDto>> GetByIdAsync(Guid id);
    Task<Result<List<ChiTieuHangNgayDto>>> GetUpdatedSince(DateTime since);
    Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto);
    Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto);
    Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id);
    Task<Result<ChiTieuHangNgayDto>> RestoreAsync(Guid id);
}
