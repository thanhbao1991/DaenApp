using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;

public class SanPhamService : ISanPhamService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SanPhamService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<SanPhamDto>> GetAllAsync()
    {
        var list = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .OrderByDescending(sp => sp.Id) // hoặc dùng ThoiGianTao nếu có
            .ToListAsync();

        return _mapper.Map<List<SanPhamDto>>(list);
    }

    public async Task<SanPhamDto?> GetByIdAsync(Guid id)
    {
        var sp = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        return _mapper.Map<SanPhamDto?>(sp);
    }

    public async Task CreateAsync(SanPhamDto dto)
    {
        var entity = _mapper.Map<SanPham>(dto);
        entity.Id = Guid.NewGuid();
        foreach (var b in entity.BienThe)
        {
            b.Id = Guid.NewGuid();
            b.IdSanPham = entity.Id;
        }

        _context.SanPhams.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Guid id, SanPhamDto dto)
    {
        var existing = await _context.SanPhams
            .Include(x => x.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (existing == null) throw new Exception("Không tìm thấy sản phẩm.");

        // cập nhật thông tin chính
        existing.Ten = dto.Ten;
        existing.MoTa = dto.MoTa;
        existing.VietTat = dto.VietTat;
        existing.DaBan = dto.DaBan;
        existing.IdNhomSanPham = dto.IdNhomSanPham;

        // cập nhật biến thể: xóa hết + thêm lại
        _context.SanPhamBienThes.RemoveRange(existing.BienThe);
        existing.BienThe = dto.BienThe.Select(b => new SanPhamBienThe
        {
            Id = Guid.NewGuid(),
            IdSanPham = existing.Id,
            TenBienThe = b.TenBienThe,
            GiaBan = b.GiaBan
        }).ToList();

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var sp = await _context.SanPhams
            .Include(x => x.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (sp == null) return;

        _context.SanPhamBienThes.RemoveRange(sp.BienThe);
        _context.SanPhams.Remove(sp);
        await _context.SaveChangesAsync();
    }
}