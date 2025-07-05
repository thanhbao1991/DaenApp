using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
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
                .Include(sp => sp.NhomSanPham)
            .ToListAsync();

        return _mapper.Map<List<SanPhamDto>>(list);
    }

    public async Task<SanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.BienThe)
                .Include(sp => sp.NhomSanPham)

            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : _mapper.Map<SanPhamDto>(entity);
    }

    public async Task<SanPhamDto> CreateAsync(SanPhamDto dto)
    {
        try
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
        catch (DbUpdateException ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
    }

    public async Task<SanPhamDto> UpdateAsync(Guid id, SanPhamDto dto)
    {
        try
        {
            var entity = await _context.SanPhams
                .Include(sp => sp.BienThe)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new Exception("Không tìm thấy sản phẩm");

            _mapper.Map(dto, entity);

            var bienTheMoi = dto.BienThe;
            var bienTheCu = entity.BienThe.ToList();

            // ✅ Kiểm tra chỉ có 1 biến thể MacDinh
            if (bienTheMoi.Count(bt => bt.MacDinh) > 1)
                throw new Exception("Mỗi sản phẩm chỉ có duy nhất một biến thể được đánh dấu là mặc định.");

            // ✅ Đặt tất cả MacDinh = false trước khi cập nhật lại
            foreach (var bt in entity.BienThe)
                bt.MacDinh = false;
            await _context.SaveChangesAsync();

            // ✅ Xoá biến thể không còn
            foreach (var btCu in bienTheCu)
            {
                if (!bienTheMoi.Any(bt => bt.Id == btCu.Id))
                {
                    bool daDuocSuDung = await _context.ChiTietHoaDons
                        .AnyAsync(ct => ct.IdSanPhamBienThe == btCu.Id);

                    if (!daDuocSuDung)
                        _context.SanPhamBienThes.Remove(btCu);
                }
            }

            // ✅ Thêm mới & cập nhật lại (sau khi reset MacDinh)
            foreach (var btMoi in bienTheMoi)
            {
                var btEntity = entity.BienThe.FirstOrDefault(x => x.Id == btMoi.Id);
                if (btEntity != null)
                {
                    btEntity.TenBienThe = btMoi.TenBienThe;
                    btEntity.GiaBan = btMoi.GiaBan;
                    btEntity.MacDinh = btMoi.MacDinh;
                }
                else
                {
                    var newBt = _mapper.Map<SanPhamBienThe>(btMoi);
                    newBt.Id = Guid.NewGuid();
                    newBt.IdSanPham = entity.Id;
                    _context.SanPhamBienThes.Add(newBt);
                }
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<SanPhamDto>(entity);
        }
        catch (DbUpdateException ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
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