using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ILocationApi
{
    Task<Result<List<LocationDto>>> GetAllAsync();
    Task<Result<LocationDto>> GetByIdAsync(Guid id);
    Task<Result<List<LocationDto>>> GetUpdatedSince(DateTime since);
    Task<Result<LocationDto>> CreateAsync(LocationDto dto);
    Task<Result<LocationDto>> UpdateAsync(Guid id, LocationDto dto);
    Task<Result<LocationDto>> DeleteAsync(Guid id);
    Task<Result<LocationDto>> RestoreAsync(Guid id);
}
