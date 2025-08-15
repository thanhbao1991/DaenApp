using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTietHoaDonNoService : IChiTietHoaDonNoService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonNo"];

    public ChiTietHoaDonNoService(AppDbContext context)
    {
        _context = context;
    }

    private ChiTietHoaDonNoDto ToDto(ChiTietHoaDonNo entity)
    {
        return new ChiTietHoaDonNoDto
        {
            Id = entity.Id,
            MaHoaDon = entity.HoaDon.MaHoaDon,
            SoTienNo = entity.SoTienNo,
            SoTienDaTra = entity.SoTienDaTra,
            NgayGio = entity.NgayGio,
            GhiChu = entity.GhiChu,
            Ngay = entity.Ngay,
            HoaDonId = entity.HoaDonId,
            KhachHangId = entity.KhachHangId,
            Ten = entity.KhachHang?.Ten, // nếu có
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            LastModified = entity.LastModified
        };
    }

    public async Task<List<ChiTietHoaDonNoDto>> GetAllAsync()
    {
        var list = await _context.ChiTietHoaDonNos.AsNoTracking()
           .Include(x => x.KhachHang)
           .Include(x => x.HoaDon)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<ChiTietHoaDonNoDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> CreateAsync(ChiTietHoaDonNoDto dto)
    {
        var entity = new ChiTietHoaDonNo
        {
            Id = Guid.NewGuid(),
            SoTienNo = dto.SoTienNo,
            SoTienDaTra = dto.SoTienDaTra,
            NgayGio = dto.NgayGio,
            GhiChu = dto.GhiChu,
            Ngay = dto.Ngay,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,

            CreatedAt = DateTime.Now,
            LastModified = DateTime.Now,
            IsDeleted = false,
        };

        _context.ChiTietHoaDonNos.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTietHoaDonNoDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> UpdateAsync(Guid id, ChiTietHoaDonNoDto dto)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTietHoaDonNoDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);

        entity.SoTienNo = dto.SoTienNo;
        entity.SoTienDaTra = dto.SoTienDaTra;
        entity.NgayGio = dto.NgayGio;
        entity.Ngay = dto.Ngay;
        entity.GhiChu = dto.GhiChu;
        entity.KhachHangId = dto.KhachHangId;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTietHoaDonNoDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        return Result<ChiTietHoaDonNoDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ChiTietHoaDonNoDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonNoDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ChiTietHoaDonNoDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTietHoaDonNoDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTietHoaDonNoDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.ChiTietHoaDonNos.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
                 .OrderByDescending(x => x.LastModified) // ✅ THÊM DÒNG NÀY
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}
