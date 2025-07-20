using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ITaiKhoanApi
{
    Task<Result<List<TaiKhoanDto>>> GetAllAsync();
    Task<Result<TaiKhoanDto>> GetByIdAsync(Guid id);
    Task<Result<List<TaiKhoanDto>>> GetUpdatedSince(DateTime since);
    Task<Result<TaiKhoanDto>> CreateAsync(TaiKhoanDto dto);
    Task<Result<TaiKhoanDto>> UpdateAsync(Guid id, TaiKhoanDto dto);
    Task<Result<TaiKhoanDto>> DeleteAsync(Guid id);
    Task<Result<TaiKhoanDto>> RestoreAsync(Guid id);
}