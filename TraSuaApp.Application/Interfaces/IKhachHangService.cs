using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface IKhachHangService
{
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto);
    Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result<KhachHangDto>> DeleteAsync(Guid id);
}