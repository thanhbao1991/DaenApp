using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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
            .Include(t => t.IdNhomSanPhams)
            .ToListAsync();

        return _mapper.Map<List<ToppingDto>>(list);
    }

    public async Task<ToppingDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(t => t.IdNhomSanPhams)
            .FirstOrDefaultAsync(t => t.Id == id);

        return entity == null ? null : _mapper.Map<ToppingDto>(entity);
    }

    public async Task<Result> CreateAsync(ToppingDto dto)
    {
        var trungTen = await _context.Toppings.AnyAsync(x => x.Ten == dto.Ten);
        if (trungTen)
            return Result.Failure("Tên topping đã tồn tại.");

        var entity = _mapper.Map<Topping>(dto);
        entity.Id = Guid.NewGuid();

        // Gán quan hệ many-to-many
        if (dto.IdNhomSanPham != null)
        {
            var nhomSanPhams = await _context.NhomSanPhams
                .Where(n => dto.IdNhomSanPham.Contains(n.Id))
                .ToListAsync();
            entity.IdNhomSanPhams = nhomSanPhams;
        }

        _context.Toppings.Add(entity);
        await _context.SaveChangesAsync();
        return Result.Success("Đã thêm topping.");
    }

    public async Task<Result> UpdateAsync(Guid id, ToppingDto dto)
    {
        var entity = await _context.Toppings
            .Include(t => t.IdNhomSanPhams)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            return Result.Failure("Topping không tồn tại.");

        var trungTen = await _context.Toppings
            .AnyAsync(x => x.Ten == dto.Ten && x.Id != id);

        if (trungTen)
            return Result.Failure("Tên topping đã tồn tại.");

        _mapper.Map(dto, entity);

        // Cập nhật quan hệ many-to-many
        entity.IdNhomSanPhams.Clear();
        if (dto.IdNhomSanPham != null)
        {
            var nhomSanPhams = await _context.NhomSanPhams
                .Where(n => dto.IdNhomSanPham.Contains(n.Id))
                .ToListAsync();
            entity.IdNhomSanPhams = nhomSanPhams;
        }

        await _context.SaveChangesAsync();
        return Result.Success("Đã cập nhật topping.");
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(t => t.IdNhomSanPhams)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            return Result.Failure("Topping không tồn tại.");

        entity.IdNhomSanPhams.Clear();
        _context.Toppings.Remove(entity);

        await _context.SaveChangesAsync();
        return Result.Success("Đã xoá topping.");
    }
}