using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IPhuongThucThanhToanApi
{
    Task<Result<List<PhuongThucThanhToanDto>>> GetAllAsync();
    Task<Result<PhuongThucThanhToanDto>> GetByIdAsync(Guid id);
    Task<Result<List<PhuongThucThanhToanDto>>> GetUpdatedSince(DateTime since);
    Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto);
    Task<Result<PhuongThucThanhToanDto>> UpdateAsync(Guid id, PhuongThucThanhToanDto dto);
    Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id);
    Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id);
}
