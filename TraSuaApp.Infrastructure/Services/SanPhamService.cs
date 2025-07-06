using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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

    public async Task<Result> CreateAsync(SanPhamDto dto)
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

            return Result.Success("Đã thêm sản phẩm.");
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(DbExceptionHelper.Handle(ex).Message);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateAsync(Guid id, SanPhamDto dto)
    {
        try
        {
            var entity = await _context.SanPhams
                .Include(sp => sp.BienThe)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result.Failure("Không tìm thấy sản phẩm.");

            _mapper.Map(dto, entity);

            var bienTheMoi = dto.BienThe;
            var bienTheCu = entity.BienThe.ToList();

            if (bienTheMoi.Count(bt => bt.MacDinh) > 1)
                return Result.Failure("Chỉ được chọn một biến thể mặc định.");

            foreach (var bt in entity.BienThe)
                bt.MacDinh = false;

            await _context.SaveChangesAsync();

            foreach (var btCu in bienTheCu)
            {
                if (!bienTheMoi.Any(bt => bt.Id == btCu.Id))
                {
                    bool daSuDung = await _context.ChiTietHoaDons
                        .AnyAsync(ct => ct.IdSanPhamBienThe == btCu.Id);

                    if (!daSuDung)
                        _context.SanPhamBienThes.Remove(btCu);
                }
            }

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
            return Result.Success("Đã cập nhật sản phẩm.");
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(DbExceptionHelper.Handle(ex).Message);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.BienThe)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result.Failure("Không tìm thấy sản phẩm.");

        _context.SanPhamBienThes.RemoveRange(entity.BienThe);
        _context.SanPhams.Remove(entity);
        await _context.SaveChangesAsync();
        return Result.Success("Đã xoá sản phẩm.");
    }
}