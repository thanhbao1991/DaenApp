using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IHoaDonService
{
    Task<List<HoaDonDto>> GetAllAsync();
    Task<HoaDonDto?> GetByIdAsync(Guid id);
    Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonDto>> DeleteAsync(Guid id);
    Task<Result<HoaDonDto>> RestoreAsync(Guid id);
    Task<List<HoaDonDto>> GetUpdatedSince(DateTime lastSync);
}
