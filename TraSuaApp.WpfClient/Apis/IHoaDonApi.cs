using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IHoaDonApi
{
    Task<Result<List<HoaDonDto>>> GetAllAsync();
    Task<Result<HoaDonDto>> GetByIdAsync(Guid id);
    Task<Result<List<HoaDonDto>>> GetUpdatedSince(DateTime since);
    Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto);
    Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto);
    Task<Result<HoaDonDto>> DeleteAsync(Guid id);
    Task<Result<HoaDonDto>> RestoreAsync(Guid id);

}
