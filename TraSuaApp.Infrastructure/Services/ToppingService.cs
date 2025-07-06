using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services;

public class ToppingService : IToppingService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public ToppingService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<ToppingDto>> GetAllAsync()
    {
        var list = await _context.Toppings
            .Include(t => t.DanhSachNhomSanPham)
            .ThenInclude(tp => tp.NhomSanPham)
            .ToListAsync();

        return _mapper.Map<List<ToppingDto>>(list);
    }

    public async Task<ToppingDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(t => t.DanhSachNhomSanPham)
            .FirstOrDefaultAsync(t => t.Id == id);

        return entity == null ? null : _mapper.Map<ToppingDto>(entity);
    }

    public async Task<ToppingDto> CreateAsync(ToppingDto dto)
    {
        var entity = _mapper.Map<Topping>(dto);
        entity.Id = Guid.NewGuid();

        // Gán danh sách liên kết nhóm sản phẩm
        entity.DanhSachNhomSanPham = dto.IdNhomSanPham?
            .Select(id => new ToppingNhomSanPham
            {
                IdTopping = entity.Id,
                IdNhomSanPham = id
            }).ToList() ?? new();

        _context.Toppings.Add(entity);
        await _context.SaveChangesAsync();

        return _mapper.Map<ToppingDto>(entity);
    }

    public async Task<bool> UpdateAsync(Guid id, ToppingDto dto)
    {
        var entity = await _context.Toppings
            .Include(t => t.DanhSachNhomSanPham)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null) return false;

        _mapper.Map(dto, entity);

        // Cập nhật lại quan hệ NhomSanPham
        entity.DanhSachNhomSanPham.Clear();
        if (dto.IdNhomSanPham != null)
        {
            foreach (var idNhom in dto.IdNhomSanPham)
            {
                entity.DanhSachNhomSanPham.Add(new ToppingNhomSanPham
                {
                    IdTopping = id,
                    IdNhomSanPham = idNhom
                });
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Toppings.FindAsync(id);
        if (entity == null) return false;

        _context.Toppings.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}