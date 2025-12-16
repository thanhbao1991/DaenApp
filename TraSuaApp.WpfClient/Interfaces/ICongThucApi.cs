using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ICongThucApi
{
    Task<Result<List<CongThucDto>>> GetAllAsync();
    Task<Result<CongThucDto>> GetByIdAsync(Guid id);
    Task<Result<List<CongThucDto>>> GetUpdatedSince(DateTime since);
    Task<Result<CongThucDto>> CreateAsync(CongThucDto dto);
    Task<Result<CongThucDto>> UpdateAsync(Guid id, CongThucDto dto);
    Task<Result<CongThucDto>> DeleteAsync(Guid id);
    Task<Result<CongThucDto>> RestoreAsync(Guid id);
}
