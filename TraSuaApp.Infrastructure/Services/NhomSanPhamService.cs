using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services;

public class NhomSanPhamService : INhomSanPhamService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public NhomSanPhamService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<NhomSanPhamDto>> GetAllAsync()
    {
        var list = await _context.NhomSanPhams
            .OrderBy(x => x.Ten)
            .ToListAsync();

        return _mapper.Map<List<NhomSanPhamDto>>(list);
    }

    public async Task<NhomSanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NhomSanPhams.FindAsync(id);
        return entity == null ? null : _mapper.Map<NhomSanPhamDto>(entity);
    }

    public async Task<NhomSanPhamDto> CreateAsync(NhomSanPhamDto dto)
    {
        try
        {
            // ✅ Kiểm tra trùng tên
            var trungTen = await _context.NhomSanPhams.AnyAsync(x => x.Ten == dto.Ten);
            if (trungTen)
                throw new Exception("Tên nhóm sản phẩm đã tồn tại.");

            var entity = _mapper.Map<NhomSanPham>(dto);
            entity.Id = Guid.NewGuid();

            _context.NhomSanPhams.Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<NhomSanPhamDto>(entity);
        }
        catch (Exception ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
    }

    public async Task<bool> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return false;

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return false;

            _context.NhomSanPhams.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
    }
}