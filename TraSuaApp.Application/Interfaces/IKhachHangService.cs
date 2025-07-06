using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface IKhachHangService
{
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<KhachHangDto> CreateAsync(KhachHangDto dto);
    Task<Result> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result> DeleteAsync(Guid id);
}