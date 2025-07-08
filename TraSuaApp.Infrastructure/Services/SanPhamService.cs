using AutoMapper;
using Microsoft.EntityFrameworkCore;
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
            .Include(sp => sp.SanPhamBienThes)
            .Include(sp => sp.IdNhomNavigation)
            .ToListAsync();

        return _mapper.Map<List<SanPhamDto>>(list);
    }

    public async Task<SanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.SanPhamBienThes)
            .Include(sp => sp.IdNhomNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : _mapper.Map<SanPhamDto>(entity);
    }

    public async Task<Result> CreateAsync(SanPhamDto dto)
    {
        try
        {
            var entity = _mapper.Map<SanPham>(dto);
            entity.Id = Guid.NewGuid();

            foreach (var bt in entity.SanPhamBienThes)
            {
                bt.Id = Guid.NewGuid();
                bt.IdSanPham = entity.Id;
            }

            _context.SanPhams.Add(entity);
            await _context.SaveChangesAsync();

            return Result.Success("Đã thêm sản phẩm.")
                .WithId(entity.Id)
                .WithAfter(_mapper.Map<SanPhamDto>(entity));
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
                .Include(sp => sp.SanPhamBienThes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result.Failure("Không tìm thấy sản phẩm.");

            var before = _mapper.Map<SanPhamDto>(entity);

            var bienTheMoi = dto.BienThe;
            var bienTheCu = entity.SanPhamBienThes.ToList();

            if (bienTheMoi.Count(bt => bt.MacDinh) > 1)
                return Result.Failure("Chỉ được chọn một biến thể mặc định.");

            _mapper.Map(dto, entity);

            // Đặt lại tất cả biến thể thành không mặc định
            foreach (var bt in entity.SanPhamBienThes)
                bt.MacDinh = false;

            await _context.SaveChangesAsync();

            // Xoá biến thể cũ không còn tồn tại
            foreach (var btCu in bienTheCu)
            {
                if (!bienTheMoi.Any(bt => bt.Id == btCu.Id))
                {
                    bool daSuDung = await _context.ChiTietHoaDons
                        .AnyAsync(ct => ct.SanPhamBienTheId == btCu.Id);

                    if (!daSuDung)
                        _context.SanPhamBienThes.Remove(btCu);
                }
            }

            // Cập nhật hoặc thêm biến thể mới
            foreach (var btMoi in bienTheMoi)
            {
                var btEntity = entity.SanPhamBienThes.FirstOrDefault(x => x.Id == btMoi.Id);
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

            var after = await GetByIdAsync(id); // Load lại bản ghi để lấy biến thể mới
            return Result.Success("Đã cập nhật sản phẩm.")
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after!);
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
            .Include(sp => sp.SanPhamBienThes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result.Failure("Không tìm thấy sản phẩm.");

        var before = _mapper.Map<SanPhamDto>(entity);

        _context.SanPhamBienThes.RemoveRange(entity.SanPhamBienThes);
        _context.SanPhams.Remove(entity);
        await _context.SaveChangesAsync();

        return Result.Success("Đã xoá sản phẩm.")
            .WithId(id)
            .WithBefore(before);
    }
}