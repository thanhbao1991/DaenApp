using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface IKhachHangService
{
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<KhachHangDto> CreateAsync(KhachHangDto dto);
    Task<bool> UpdateAsync(Guid id, KhachHangDto dto);
    Task<bool> DeleteAsync(Guid id);
}