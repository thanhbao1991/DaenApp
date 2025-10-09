using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ITuDienTraCuuApi
{
    Task<Result<List<TuDienTraCuuDto>>> GetAllAsync();
    Task<Result<TuDienTraCuuDto>> GetByIdAsync(Guid id);
    Task<Result<List<TuDienTraCuuDto>>> GetUpdatedSince(DateTime since);
    Task<Result<TuDienTraCuuDto>> CreateAsync(TuDienTraCuuDto dto);
    Task<Result<TuDienTraCuuDto>> UpdateAsync(Guid id, TuDienTraCuuDto dto);
    Task<Result<TuDienTraCuuDto>> DeleteAsync(Guid id);
    Task<Result<TuDienTraCuuDto>> RestoreAsync(Guid id);
}
