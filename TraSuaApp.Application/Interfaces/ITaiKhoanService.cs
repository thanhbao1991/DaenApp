using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface ITaiKhoanService
{
    Task<List<TaiKhoanDto>> GetAllAsync();
    Task<TaiKhoanDto?> GetByIdAsync(Guid id);
    Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto);
    Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto);
    Task<Result<TaiKhoanDto>> DeleteAsync(Guid id);
    Task<Result<TaiKhoanDto>> RestoreAsync(Guid id);
    Task<List<TaiKhoanDto>> GetUpdatedSince(DateTime lastSync);
}