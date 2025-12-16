using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ICongThucService
{
    Task<List<CongThucDto>> GetAllAsync();
    Task<CongThucDto?> GetByIdAsync(Guid id);
    Task<Result<CongThucDto>> CreateAsync(CongThucDto dto);
    Task<Result<CongThucDto>> UpdateAsync(Guid id, CongThucDto dto);
    Task<Result<CongThucDto>> DeleteAsync(Guid id);
    Task<Result<CongThucDto>> RestoreAsync(Guid id);
    Task<List<CongThucDto>> GetUpdatedSince(DateTime lastSync);
}
