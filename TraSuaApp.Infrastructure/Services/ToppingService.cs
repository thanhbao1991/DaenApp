using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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

    public async Task<Result<ToppingDto>> CreateAsync(ToppingDto dto)
    {
        if (await _context.Toppings.AnyAsync(x => x.Ten == dto.Ten))
            return Result<ToppingDto>.Failure("Tên topping đã tồn tại.");

        var entity = _mapper.Map<Topping>(dto);
        entity.Id = Guid.NewGuid();

        if (dto.IdNhomSanPham != null)
        {
            var nhomSanPhams = await _context.NhomSanPhams
                .Where(n => dto.IdNhomSanPham.Contains(n.Id))
                .ToListAsync();

            entity.IdNhomSanPhams = nhomSanPhams;
        }

        _context.Toppings.Add(entity);
        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<ToppingDto>(entity);
        return Result<ToppingDto>.Success("Đã thêm topping.", resultDto)
            .WithId(entity.Id)
            .WithAfter(resultDto);
    }

    public async Task<Result<ToppingDto>> UpdateAsync(Guid id, ToppingDto dto)
    {
        var entity = await _context.Toppings
            .Include(t => t.IdNhomSanPhams)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure("Topping không tồn tại.");

        if (await _context.Toppings.AnyAsync(x => x.Ten == dto.Ten && x.Id != id))
            return Result<ToppingDto>.Failure("Tên topping đã tồn tại.");

        var before = _mapper.Map<ToppingDto>(entity);

        _mapper.Map(dto, entity);

        entity.IdNhomSanPhams.Clear();
        if (dto.IdNhomSanPham != null)
        {
            var nhomSanPhams = await _context.NhomSanPhams
                .Where(n => dto.IdNhomSanPham.Contains(n.Id))
                .ToListAsync();

            entity.IdNhomSanPhams = nhomSanPhams;
        }

        await _context.SaveChangesAsync();

        var resultDto = _mapper.Map<ToppingDto>(entity);
        return Result<ToppingDto>.Success("Đã cập nhật topping.", resultDto)
            .WithId(id)
            .WithBefore(before)
            .WithAfter(resultDto);
    }

    public async Task<Result<ToppingDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.Toppings
            .Include(t => t.IdNhomSanPhams)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            return Result<ToppingDto>.Failure("Topping không tồn tại.");

        var before = _mapper.Map<ToppingDto>(entity);

        entity.IdNhomSanPhams.Clear();
        _context.Toppings.Remove(entity);

        await _context.SaveChangesAsync();

        return Result<ToppingDto>.Success("Đã xoá topping.", before)
            .WithId(id)
            .WithBefore(before);
    }
}