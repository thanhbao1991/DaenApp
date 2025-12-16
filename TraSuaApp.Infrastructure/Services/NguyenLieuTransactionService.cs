using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class NguyenLieuTransactionService : INguyenLieuTransactionService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuTransaction"];

    public NguyenLieuTransactionService(AppDbContext context)
    {
        _context = context;
    }

    private static NguyenLieuTransactionDto ToDto(NguyenLieuTransaction e, NguyenLieuBanHang? nlbh = null)
    {
        return new NguyenLieuTransactionDto
        {
            Id = e.Id,

            NguyenLieuId = e.NguyenLieuId,
            TenNguyenLieu = nlbh?.Ten,
            DonViTinh = nlbh?.DonViTinh,

            NgayGio = e.NgayGio,
            Loai = e.Loai,
            SoLuong = e.SoLuong,
            DonGia = e.DonGia,
            GhiChu = e.GhiChu,

            ChiTieuHangNgayId = e.ChiTieuHangNgayId,
            HoaDonId = e.HoaDonId,

            CreatedAt = e.CreatedAt,
            LastModified = e.LastModified,
            DeletedAt = e.DeletedAt,
            IsDeleted = e.IsDeleted
        };
    }

    private IQueryable<NguyenLieuTransaction> BaseQuery()
    {
        return _context.NguyenLieuTransactions
            .Include(x => x.NguyenLieu); // NguyenLieuBanHang
    }

    public async Task<List<NguyenLieuTransactionDto>> GetAllAsync()
    {
        var list = await BaseQuery()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.NgayGio)
            .ThenByDescending(x => x.LastModified ?? x.CreatedAt)
            .ToListAsync();

        return list.Select(x => ToDto(x, x.NguyenLieu)).ToList();
    }

    public async Task<NguyenLieuTransactionDto?> GetByIdAsync(Guid id)
    {
        var e = await BaseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return e == null ? null : ToDto(e, e.NguyenLieu);
    }

    public async Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto)
    {
        if (dto.NguyenLieuId == Guid.Empty)
            return Result<NguyenLieuTransactionDto>.Failure("Vui lòng chọn nguyên liệu.");

        if (dto.SoLuong == 0)
            return Result<NguyenLieuTransactionDto>.Failure("Số lượng phải khác 0.");

        var now = DateTime.Now;

        var entity = new NguyenLieuTransaction
        {
            Id = Guid.NewGuid(),
            NguyenLieuId = dto.NguyenLieuId,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,
            Loai = dto.Loai,
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            GhiChu = dto.GhiChu,
            ChiTieuHangNgayId = dto.ChiTieuHangNgayId,
            HoaDonId = dto.HoaDonId,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.NguyenLieuTransactions.Add(entity);
        await _context.SaveChangesAsync();

        var loaded = await BaseQuery().FirstAsync(x => x.Id == entity.Id);
        var after = ToDto(loaded, loaded.NguyenLieu);

        return Result<NguyenLieuTransactionDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto)
    {
        var entity = await _context.NguyenLieuTransactions
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<NguyenLieuTransactionDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        if (dto.NguyenLieuId == Guid.Empty)
            return Result<NguyenLieuTransactionDto>.Failure("Vui lòng chọn nguyên liệu.");

        if (dto.SoLuong == 0)
            return Result<NguyenLieuTransactionDto>.Failure("Số lượng phải khác 0.");

        var beforeLoaded = await BaseQuery().AsNoTracking().FirstAsync(x => x.Id == id);
        var before = ToDto(beforeLoaded, beforeLoaded.NguyenLieu);

        var now = DateTime.Now;

        entity.NguyenLieuId = dto.NguyenLieuId;
        entity.NgayGio = dto.NgayGio == default ? entity.NgayGio : dto.NgayGio;
        entity.Loai = dto.Loai;
        entity.SoLuong = dto.SoLuong;
        entity.DonGia = dto.DonGia;
        entity.GhiChu = dto.GhiChu;
        entity.ChiTieuHangNgayId = dto.ChiTieuHangNgayId;
        entity.HoaDonId = dto.HoaDonId;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var loaded = await BaseQuery().FirstAsync(x => x.Id == id);
        var after = ToDto(loaded, loaded.NguyenLieu);

        return Result<NguyenLieuTransactionDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.NguyenLieuTransactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null || entity.IsDeleted)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var beforeLoaded = await BaseQuery().AsNoTracking().FirstAsync(x => x.Id == id);
        var before = ToDto(beforeLoaded, beforeLoaded.NguyenLieu);

        var now = DateTime.Now;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<NguyenLieuTransactionDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.NguyenLieuTransactions.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return Result<NguyenLieuTransactionDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<NguyenLieuTransactionDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        var now = DateTime.Now;
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var loaded = await BaseQuery().FirstAsync(x => x.Id == id);
        var after = ToDto(loaded, loaded.NguyenLieu);

        return Result<NguyenLieuTransactionDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<NguyenLieuTransactionDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await BaseQuery()
            .AsNoTracking()
            .Where(x => (x.LastModified ?? x.CreatedAt) > lastSync)
            .OrderByDescending(x => x.LastModified ?? x.CreatedAt)
            .ToListAsync();

        return list.Select(x => ToDto(x, x.NguyenLieu)).ToList();
    }
}
