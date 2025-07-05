
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces;

public interface INhomSanPhamService
{
    Task<List<NhomSanPhamDto>> GetAllAsync();
    Task<NhomSanPhamDto?> GetByIdAsync(Guid id);
    Task<NhomSanPhamDto> CreateAsync(NhomSanPhamDto dto);
    Task<bool> UpdateAsync(Guid id, NhomSanPhamDto dto);
    Task<bool> DeleteAsync(Guid id);
}