using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ICongViecNoiBoService
{
    Task<List<CongViecNoiBoDto>> GetAllAsync();
    Task<CongViecNoiBoDto?> GetByIdAsync(Guid id);
    Task<Result<CongViecNoiBoDto>> CreateAsync(CongViecNoiBoDto dto);
    Task<Result<CongViecNoiBoDto>> UpdateAsync(Guid id, CongViecNoiBoDto dto);
    Task<Result<CongViecNoiBoDto>> DeleteAsync(Guid id);
    Task<Result<CongViecNoiBoDto>> RestoreAsync(Guid id);
    Task<List<CongViecNoiBoDto>> GetUpdatedSince(DateTime lastSync);
}
