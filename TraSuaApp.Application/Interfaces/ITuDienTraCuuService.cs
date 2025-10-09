using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ITuDienTraCuuService
{
    Task<List<TuDienTraCuuDto>> GetAllAsync();
    Task<TuDienTraCuuDto?> GetByIdAsync(Guid id);
    Task<Result<TuDienTraCuuDto>> CreateAsync(TuDienTraCuuDto dto);
    Task<Result<TuDienTraCuuDto>> UpdateAsync(Guid id, TuDienTraCuuDto dto);
    Task<Result<TuDienTraCuuDto>> DeleteAsync(Guid id);
    Task<Result<TuDienTraCuuDto>> RestoreAsync(Guid id);
    Task<List<TuDienTraCuuDto>> GetUpdatedSince(DateTime lastSync);
}
