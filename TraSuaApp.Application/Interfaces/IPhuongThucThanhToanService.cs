using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IPhuongThucThanhToanService
{
    Task<List<PhuongThucThanhToanDto>> GetAllAsync();
    Task<PhuongThucThanhToanDto?> GetByIdAsync(Guid id);
    Task<Result<PhuongThucThanhToanDto>> CreateAsync(PhuongThucThanhToanDto dto);
    Task<Result<PhuongThucThanhToanDto>> UpdateAsync(Guid id, PhuongThucThanhToanDto dto);
    Task<Result<PhuongThucThanhToanDto>> DeleteAsync(Guid id);
    Task<Result<PhuongThucThanhToanDto>> RestoreAsync(Guid id);
    Task<List<PhuongThucThanhToanDto>> GetUpdatedSince(DateTime lastSync);
}
