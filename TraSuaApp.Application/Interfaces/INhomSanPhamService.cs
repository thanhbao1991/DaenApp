using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Application.Interfaces;

public interface INhomSanPhamService
{
    Task<List<NhomSanPhamDto>> GetAllAsync();
    Task<NhomSanPhamDto?> GetByIdAsync(Guid id);
    Task<Result> CreateAsync(NhomSanPhamDto dto);
    Task<Result> UpdateAsync(Guid id, NhomSanPhamDto dto);
    Task<Result> DeleteAsync(Guid id);
}