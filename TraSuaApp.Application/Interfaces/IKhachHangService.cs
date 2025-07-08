using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public interface IKhachHangService
{
    Task<List<KhachHangDto>> GetAllAsync();
    Task<KhachHangDto?> GetByIdAsync(Guid id);
    Task<Result> CreateAsync(KhachHangDto dto); // sửa kiểu trả về
    Task<Result> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result> DeleteAsync(Guid id);
}