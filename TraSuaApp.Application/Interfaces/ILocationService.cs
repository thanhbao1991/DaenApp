using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ILocationService
{
    Task<List<LocationDto>> GetAllAsync();
    Task<LocationDto?> GetByIdAsync(Guid id);
    Task<Result<LocationDto>> CreateAsync(LocationDto dto);
    Task<Result<LocationDto>> UpdateAsync(Guid id, LocationDto dto);
    Task<Result<LocationDto>> DeleteAsync(Guid id);
    Task<Result<LocationDto>> RestoreAsync(Guid id);
    Task<List<LocationDto>> GetUpdatedSince(DateTime lastSync);
}
