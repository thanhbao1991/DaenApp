using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class VoucherService : IVoucherService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["Voucher"];

    public VoucherService(AppDbContext context)
    {
        _context = context;
    }

    private static VoucherDto ToDto(Voucher entity)
    {
        return new VoucherDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            GiaTri = entity.GiaTri,
            KieuGiam = entity.KieuGiam,
            DieuKienToiThieu = entity.DieuKienToiThieu,
            SoLanSuDungToiDa = entity.SoLanSuDungToiDa,
            NgayBatDau = entity.NgayBatDau,
            NgayKetThuc = entity.NgayKetThuc,
            DangSuDung = entity.DangSuDung,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<VoucherDto>> GetAllAsync()
    {
        return await _context.Vouchers.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<VoucherDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<VoucherDto>> CreateAsync(VoucherDto dto)
    {
        bool daTonTai = await _context.Vouchers
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<VoucherDto>.Failure($"{dto.Ten} đã tồn tại.");

        var now = DateTime.Now;
        var entity = new Voucher
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            KieuGiam = dto.KieuGiam,
            GiaTri = dto.GiaTri,
            DieuKienToiThieu = dto.DieuKienToiThieu,
            SoLanSuDungToiDa = dto.SoLanSuDungToiDa,
            NgayBatDau = dto.NgayBatDau,
            NgayKetThuc = dto.NgayKetThuc,
            DangSuDung = dto.DangSuDung,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.Vouchers.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<VoucherDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<VoucherDto>> UpdateAsync(Guid id, VoucherDto dto)
    {
        var entity = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<VoucherDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<VoucherDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.Vouchers
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<VoucherDto>.Failure($"{dto.Ten} đã tồn tại.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.Ten = dto.Ten.Trim();
        entity.KieuGiam = dto.KieuGiam;
        entity.GiaTri = dto.GiaTri;
        entity.DieuKienToiThieu = dto.DieuKienToiThieu;
        entity.SoLanSuDungToiDa = dto.SoLanSuDungToiDa;
        entity.NgayBatDau = dto.NgayBatDau;
        entity.NgayKetThuc = dto.NgayKetThuc;
        entity.DangSuDung = dto.DangSuDung;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<VoucherDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<VoucherDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<VoucherDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<VoucherDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<VoucherDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.Vouchers
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<VoucherDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<VoucherDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<VoucherDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<VoucherDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.Vouchers.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}