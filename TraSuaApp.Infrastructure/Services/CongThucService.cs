using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class CongThucService : ICongThucService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

    public CongThucService(AppDbContext context)
    {
        _context = context;
    }

    private static CongThucDto ToDto(CongThuc entity)
    {
        return new CongThucDto
        {
            Id = entity.Id,
            SanPhamBienTheId = entity.SanPhamBienTheId,
            Ten = entity.Ten,
            Loai = entity.Loai,
            IsDefault = entity.IsDefault,

            TenSanPham = entity.SanPhamBienThe?.SanPham?.Ten,
            TenBienThe = entity.SanPhamBienThe?.TenBienThe,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted
        };
    }

    public async Task<List<CongThucDto>> GetAllAsync()
    {
        return await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<CongThucDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<CongThucDto>> CreateAsync(CongThucDto dto)
    {
        // Cùng SanPhamBienTheId + Ten (case-insensitive) không được trùng
        bool daTonTai = await _context.CongThucs
            .AnyAsync(x =>
                x.SanPhamBienTheId == dto.SanPhamBienTheId &&
                !x.IsDeleted &&
                (x.Ten ?? "").ToLower() == (dto.Ten ?? "").Trim().ToLower());

        if (daTonTai)
            return Result<CongThucDto>.Failure(
                $"{_friendlyName} '{dto.Ten}' cho biến thể này đã tồn tại.");

        var now = DateTime.Now;

        var entity = new CongThuc
        {
            Id = Guid.NewGuid(),
            SanPhamBienTheId = dto.SanPhamBienTheId,
            Ten = dto.Ten?.Trim(),
            Loai = dto.Loai?.Trim(),
            IsDefault = dto.IsDefault,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        // Nếu chọn IsDefault = true thì tắt default các công thức khác của cùng biến thể
        if (entity.IsDefault)
        {
            var others = await _context.CongThucs
                .Where(x => x.SanPhamBienTheId == entity.SanPhamBienTheId &&
                            !x.IsDeleted &&
                            x.IsDefault)
                .ToListAsync();

            foreach (var ct in others)
            {
                ct.IsDefault = false;
                ct.LastModified = now;
            }
        }

        _context.CongThucs.Add(entity);
        await _context.SaveChangesAsync();

        // load lại để có TenSanPham / TenBienThe
        entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .FirstAsync(x => x.Id == entity.Id);

        var after = ToDto(entity);
        return Result<CongThucDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<CongThucDto>> UpdateAsync(Guid id, CongThucDto dto)
    {
        var entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<CongThucDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<CongThucDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.CongThucs
            .AnyAsync(x =>
                x.Id != id &&
                x.SanPhamBienTheId == dto.SanPhamBienTheId &&
                !x.IsDeleted &&
                (x.Ten ?? "").ToLower() == (dto.Ten ?? "").Trim().ToLower());

        if (daTonTai)
            return Result<CongThucDto>.Failure(
                $"{_friendlyName} '{dto.Ten}' cho biến thể này đã tồn tại.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.SanPhamBienTheId = dto.SanPhamBienTheId;
        entity.Ten = dto.Ten?.Trim();
        entity.Loai = dto.Loai?.Trim();
        entity.IsDefault = dto.IsDefault;
        entity.LastModified = now;

        if (entity.IsDefault)
        {
            var others = await _context.CongThucs
                .Where(x => x.Id != entity.Id &&
                            x.SanPhamBienTheId == entity.SanPhamBienTheId &&
                            !x.IsDeleted &&
                            x.IsDefault)
                .ToListAsync();

            foreach (var ct in others)
            {
                ct.IsDefault = false;
                ct.LastModified = now;
            }
        }

        await _context.SaveChangesAsync();

        // reload
        entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(entity);
        return Result<CongThucDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<CongThucDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<CongThucDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<CongThucDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<CongThucDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<CongThucDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<CongThucDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<CongThucDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<CongThucDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.CongThucs
            .Include(x => x.SanPhamBienThe)
                .ThenInclude(bt => bt.SanPham)
            .AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}