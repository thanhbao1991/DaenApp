using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTieuHangNgayService : IChiTieuHangNgayService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTieuHangNgay"];

    public ChiTieuHangNgayService(AppDbContext context)
    {
        _context = context;
    }


    private ChiTieuHangNgayDto ToDto(ChiTieuHangNgay entity)
    {
        return new ChiTieuHangNgayDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            DonGia = entity.DonGia,
            SoLuong = entity.SoLuong,
            ThanhTien = entity.ThanhTien,
            Ngay = entity.Ngay,
            NgayGio = entity.NgayGio,
            NguyenLieuId = entity.NguyenLieuId,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            BillThang = entity.BillThang // hoặc lấy từ entity nếu có cột này
        };
    }
    public async Task<List<ChiTieuHangNgayDto>> GetAllAsync()
    {
        var list = await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<ChiTieuHangNgayDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto)
    {
        var now = DateTime.Now;
        var entity = new ChiTieuHangNgay
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            BillThang = dto.BillThang,
            ThanhTien = dto.ThanhTien,
            Ngay = now.Date,
            NgayGio = now,
            NguyenLieuId = dto.NguyenLieuId,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.ChiTieuHangNgays.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTieuHangNgayDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTieuHangNgayDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var before = ToDto(entity);
        var now = DateTime.Now;
        entity.Ten = dto.Ten;
        entity.SoLuong = dto.SoLuong;
        entity.BillThang = dto.BillThang;
        entity.DonGia = dto.DonGia;
        entity.ThanhTien = dto.ThanhTien;
        //entity.Ngay = now.Date;
        //entity.NgayGio = now;
        entity.NguyenLieuId = dto.NguyenLieuId;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTieuHangNgayDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<ChiTieuHangNgayDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<ChiTieuHangNgayDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ChiTieuHangNgayDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ChiTieuHangNgayDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<ChiTieuHangNgayDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTieuHangNgayDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
                 .OrderByDescending(x => x.LastModified) // ✅ THÊM DÒNG NÀY
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}
