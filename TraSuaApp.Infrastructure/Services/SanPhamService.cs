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
            .ToListAsync();

        return _mapper.Map<List<SanPhamDto>>(list);
    }

    public async Task<SanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : _mapper.Map<SanPhamDto>(entity);
    }

    public async Task<SanPhamDto> CreateAsync(SanPhamDto dto)
    {
        var entity = _mapper.Map<SanPham>(dto);
        entity.Id = Guid.NewGuid();
        foreach (var bt in entity.BienThe)
        {
            bt.Id = Guid.NewGuid();
            bt.IdSanPham = entity.Id;
        }

        _context.SanPhams.Add(entity);
        await _context.SaveChangesAsync();
        return _mapper.Map<SanPhamDto>(entity);
    }

    public async Task<SanPhamDto> UpdateAsync(Guid id, SanPhamDto dto)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) throw new Exception("Không tìm thấy sản phẩm");

        _mapper.Map(dto, entity);

        var bienTheMoi = dto.BienThe;
        var bienTheCu = entity.BienThe.ToList();

        // 1. Xóa biến thể không còn tồn tại trong DTO (nếu chưa từng dùng trong hóa đơn)
        foreach (var btCu in bienTheCu)
        {
            if (!bienTheMoi.Any(bt => bt.Id == btCu.Id))
            {
                // Kiểm tra xem đã có dữ liệu trong ChiTietHoaDons chưa
                bool daDuocSuDung = await _context.ChiTietHoaDons
                    .AnyAsync(ct => ct.IdSanPhamBienThe == btCu.Id);

                if (!daDuocSuDung)
                    _context.SanPhamBienThes.Remove(btCu);
                else
                    continue; // giữ lại nếu đã được dùng
            }
        }

        // 2. Cập nhật hoặc thêm mới biến thể
        foreach (var btMoi in bienTheMoi)
        {
            var btEntity = entity.BienThe.FirstOrDefault(x => x.Id == btMoi.Id);
            if (btEntity != null)
            {
                // Cập nhật
                btEntity.TenBienThe = btMoi.TenBienThe;
                btEntity.GiaBan = btMoi.GiaBan;
            }
            else
            {
                // Thêm mới
                var newBt = _mapper.Map<SanPhamBienThe>(btMoi);
                newBt.Id = Guid.NewGuid(); // bạn có thể để nguyên nếu btMoi.Id đã là GUID mới
                newBt.IdSanPham = entity.Id;
                _context.SanPhamBienThes.Add(newBt);
            }
        }

        await _context.SaveChangesAsync();
        return _mapper.Map<SanPhamDto>(entity);
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        _context.SanPhamBienThes.RemoveRange(entity.BienThe);
        _context.SanPhams.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}