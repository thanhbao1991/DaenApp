using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class SuDungNguyenLieuService : ISuDungNguyenLieuService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["SuDungNguyenLieu"];

    public SuDungNguyenLieuService(AppDbContext context)
    {
        _context = context;
    }

    private static SuDungNguyenLieuDto ToDto(SuDungNguyenLieu entity)
    {
        var sp = entity.CongThuc?.SanPhamBienThe?.SanPham;
        var bt = entity.CongThuc?.SanPhamBienThe;
        var nl = entity.NguyenLieu;

        return new SuDungNguyenLieuDto
        {
            Id = entity.Id,
            CongThucId = entity.CongThucId,
            NguyenLieuId = entity.NguyenLieuId,
            SoLuong = entity.SoLuong,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,

            TenSanPham = sp?.Ten,
            TenBienThe = bt?.TenBienThe,
            TenNguyenLieu = nl?.Ten,
            DonViTinh = nl?.DonViTinh
        };
    }

    private IQueryable<SuDungNguyenLieu> BaseQuery()
    {
        return _context.SuDungNguyenLieus
            .Include(x => x.CongThuc)
                .ThenInclude(c => c.SanPhamBienThe)
                    .ThenInclude(bt => bt.SanPham)
            .Include(x => x.NguyenLieu);
    }

    public async Task<List<SuDungNguyenLieuDto>> GetAllAsync()
    {
        var list = await BaseQuery()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified ?? x.CreatedAt)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<SuDungNguyenLieuDto?> GetByIdAsync(Guid id)
    {
        var entity = await BaseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<SuDungNguyenLieuDto>> CreateAsync(SuDungNguyenLieuDto dto)
    {
        if (dto.CongThucId == Guid.Empty || dto.NguyenLieuId == Guid.Empty)
            return Result<SuDungNguyenLieuDto>.Failure("Vui lòng chọn công thức và nguyên liệu.");

        bool daTonTai = await _context.SuDungNguyenLieus
            .AnyAsync(x =>
                x.CongThucId == dto.CongThucId &&
                x.NguyenLieuId == dto.NguyenLieuId &&
                !x.IsDeleted);

        if (daTonTai)
            return Result<SuDungNguyenLieuDto>.Failure("Nguyên liệu này đã được khai báo trong công thức.");

        var now = DateTime.Now;

        var entity = new SuDungNguyenLieu
        {
            Id = Guid.NewGuid(),
            CongThucId = dto.CongThucId,
            NguyenLieuId = dto.NguyenLieuId,
            SoLuong = dto.SoLuong,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.SuDungNguyenLieus.Add(entity);
        await _context.SaveChangesAsync();

        // Reload with navigation for full DTO
        entity = await BaseQuery().FirstAsync(x => x.Id == entity.Id);

        var after = ToDto(entity);
        return Result<SuDungNguyenLieuDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<SuDungNguyenLieuDto>> UpdateAsync(Guid id, SuDungNguyenLieuDto dto)
    {
        var entity = await _context.SuDungNguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<SuDungNguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<SuDungNguyenLieuDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        if (dto.CongThucId == Guid.Empty || dto.NguyenLieuId == Guid.Empty)
            return Result<SuDungNguyenLieuDto>.Failure("Vui lòng chọn công thức và nguyên liệu.");

        bool daTonTai = await _context.SuDungNguyenLieus
            .AnyAsync(x =>
                x.Id != id &&
                x.CongThucId == dto.CongThucId &&
                x.NguyenLieuId == dto.NguyenLieuId &&
                !x.IsDeleted);

        if (daTonTai)
            return Result<SuDungNguyenLieuDto>.Failure("Nguyên liệu này đã được khai báo trong công thức.");

        var now = DateTime.Now;

        entity.CongThucId = dto.CongThucId;
        entity.NguyenLieuId = dto.NguyenLieuId;
        entity.SoLuong = dto.SoLuong;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var reloaded = await BaseQuery().FirstAsync(x => x.Id == entity.Id);
        var after = ToDto(reloaded);

        return Result<SuDungNguyenLieuDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithAfter(after);
    }

    public async Task<Result<SuDungNguyenLieuDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.SuDungNguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<SuDungNguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto((await BaseQuery().FirstAsync(x => x.Id == id)));
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<SuDungNguyenLieuDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<SuDungNguyenLieuDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.SuDungNguyenLieus
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<SuDungNguyenLieuDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<SuDungNguyenLieuDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var reloaded = await BaseQuery().FirstAsync(x => x.Id == entity.Id);
        var after = ToDto(reloaded);

        return Result<SuDungNguyenLieuDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<SuDungNguyenLieuDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await BaseQuery()
            .AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}