using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ICongViecNoiBoApi
{
    Task<Result<List<CongViecNoiBoDto>>> GetAllAsync();
    Task<Result<CongViecNoiBoDto>> GetByIdAsync(Guid id);
    Task<Result<List<CongViecNoiBoDto>>> GetUpdatedSince(DateTime since);
    Task<Result<CongViecNoiBoDto>> CreateAsync(CongViecNoiBoDto dto);
    Task<Result<CongViecNoiBoDto>> UpdateAsync(Guid id, CongViecNoiBoDto dto);
    Task<Result<CongViecNoiBoDto>> DeleteAsync(Guid id);
    Task<Result<CongViecNoiBoDto>> RestoreAsync(Guid id);
}
