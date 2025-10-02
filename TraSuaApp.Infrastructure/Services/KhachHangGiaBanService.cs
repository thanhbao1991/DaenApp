using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class KhachHangGiaBanService : IKhachHangGiaBanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHangGiaBan"];

    public KhachHangGiaBanService(AppDbContext context)
    {
        _context = context;
    }

    private static KhachHangGiaBanDto ToDto(KhachHangGiaBan entity)
    {
        return new KhachHangGiaBanDto
        {
            Id = entity.Id,
            KhachHangId = entity.KhachHangId,
            SanPhamBienTheId = entity.SanPhamBienTheId,
            GiaBan = entity.GiaBan,

            // Thông tin hiển thị
            TenKhachHang = entity.KhachHang?.Ten,
            TenSanPham = entity.SanPhamBienThe?.SanPham?.Ten,
            TenBienThe = entity.SanPhamBienThe?.TenBienThe,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<KhachHangGiaBanDto>> GetAllAsync()
    {
        return await _context.KhachHangGiaBans.AsNoTracking()
            .Include(x => x.KhachHang)
            .Include(x => x.SanPhamBienThe).ThenInclude(bt => bt.SanPham)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<KhachHangGiaBanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.KhachHangGiaBans
            .AsNoTracking()
            .Include(x => x.KhachHang)
            .Include(x => x.SanPhamBienThe).ThenInclude(bt => bt.SanPham)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<KhachHangGiaBanDto>> CreateAsync(KhachHangGiaBanDto dto)
    {
        var now = DateTime.Now;
        var entity = new KhachHangGiaBan
        {
            Id = Guid.NewGuid(),
            KhachHangId = dto.KhachHangId,
            SanPhamBienTheId = dto.SanPhamBienTheId,
            GiaBan = dto.GiaBan,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.KhachHangGiaBans.Add(entity);
        await _context.SaveChangesAsync();

        // nạp lại navigation để map tên
        await _context.Entry(entity).Reference(e => e.KhachHang).LoadAsync();
        await _context.Entry(entity).Reference(e => e.SanPhamBienThe).LoadAsync();
        if (entity.SanPhamBienThe != null)
            await _context.Entry(entity.SanPhamBienThe).Reference(bt => bt.SanPham).LoadAsync();

        var after = ToDto(entity);
        return Result<KhachHangGiaBanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<KhachHangGiaBanDto>> UpdateAsync(Guid id, KhachHangGiaBanDto dto)
    {
        var entity = await _context.KhachHangGiaBans
            .Include(x => x.KhachHang)
            .Include(x => x.SanPhamBienThe).ThenInclude(bt => bt.SanPham)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<KhachHangGiaBanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<KhachHangGiaBanDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);

        entity.KhachHangId = dto.KhachHangId;
        entity.SanPhamBienTheId = dto.SanPhamBienTheId;
        entity.GiaBan = dto.GiaBan;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangGiaBanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<KhachHangGiaBanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.KhachHangGiaBans.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<KhachHangGiaBanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<KhachHangGiaBanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<KhachHangGiaBanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.KhachHangGiaBans.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangGiaBanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<KhachHangGiaBanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangGiaBanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<KhachHangGiaBanDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.KhachHangGiaBans.AsNoTracking()
            .Include(x => x.KhachHang)
            .Include(x => x.SanPhamBienThe).ThenInclude(bt => bt.SanPham)
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}