using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface ITaiKhoanService
{
    Task<List<TaiKhoanDto>> GetAllAsync();
    Task<TaiKhoanDto?> GetByIdAsync(Guid id);
    Task<Result> CreateAsync(TaiKhoanDto dto);
    Task<Result> UpdateAsync(Guid id, TaiKhoanDto dto);
    Task<Result> DeleteAsync(Guid id);
}