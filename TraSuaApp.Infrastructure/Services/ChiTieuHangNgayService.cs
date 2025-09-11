using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
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
            GhiChu = entity.GhiChu,
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

    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto)
    {
        var now = DateTime.Now;
        var entity = new ChiTieuHangNgay
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            GhiChu = dto.GhiChu,
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            BillThang = dto.BillThang,
            ThanhTien = dto.ThanhTien,
            Ngay = dto.Ngay,
            NgayGio = dto.NgayGio,
            NguyenLieuId = dto.NguyenLieuId,
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.ChiTieuHangNgays.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        // Cập nhật giá nhập nguyên liệu
        await UpdateNguyenLieuGiaNhapAsync(dto.NguyenLieuId, dto.DonGia);

        return Result<ChiTieuHangNgayDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    private async Task UpdateNguyenLieuGiaNhapAsync(Guid nguyenLieuId, decimal donGia)
    {
        var nguyenLieu = await _context.NguyenLieus
            .FirstOrDefaultAsync(nl => nl.Id == nguyenLieuId);

        if (nguyenLieu != null && nguyenLieu.GiaNhap != donGia)
        {
            nguyenLieu.GiaNhap = donGia;
            nguyenLieu.LastModified = DateTime.Now;
            await _context.SaveChangesAsync();
        }
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
        entity.GhiChu = dto.GhiChu;
        entity.SoLuong = dto.SoLuong;
        entity.BillThang = dto.BillThang;
        entity.DonGia = dto.DonGia;
        entity.ThanhTien = dto.ThanhTien;
        entity.Ngay = dto.Ngay;
        entity.NgayGio = dto.NgayGio;
        entity.NguyenLieuId = dto.NguyenLieuId;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        // Cập nhật giá nhập nguyên liệu
        await UpdateNguyenLieuGiaNhapAsync(dto.NguyenLieuId, dto.DonGia);


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

    public async Task<List<ChiTieuHangNgayDto>> GetAllAsync()
    {
        var today = DateTime.Today;
        var fromDate = today.AddDays(-2);

        return await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= fromDate)
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTieuHangNgayDto
            {
                Id = x.Id,
                Ten = x.Ten,
                DonGia = x.DonGia,
                SoLuong = x.SoLuong,
                GhiChu = x.GhiChu,
                ThanhTien = x.ThanhTien,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                NguyenLieuId = x.NguyenLieuId,
                CreatedAt = x.CreatedAt,
                LastModified = x.LastModified,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                BillThang = x.BillThang
            })
            .ToListAsync();
    }
    public async Task<ChiTieuHangNgayDto?> GetByIdAsync(Guid id)
    {
        return await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new ChiTieuHangNgayDto
            {
                Id = x.Id,
                Ten = x.Ten,
                DonGia = x.DonGia,
                SoLuong = x.SoLuong,
                GhiChu = x.GhiChu,
                ThanhTien = x.ThanhTien,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                NguyenLieuId = x.NguyenLieuId,
                CreatedAt = x.CreatedAt,
                LastModified = x.LastModified,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                BillThang = x.BillThang
            })
            .FirstOrDefaultAsync();
    }
    public async Task<List<ChiTieuHangNgayDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.ChiTieuHangNgays.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => new ChiTieuHangNgayDto
            {
                Id = x.Id,
                Ten = x.Ten,
                DonGia = x.DonGia,
                SoLuong = x.SoLuong,
                GhiChu = x.GhiChu,
                ThanhTien = x.ThanhTien,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                NguyenLieuId = x.NguyenLieuId,
                CreatedAt = x.CreatedAt,
                LastModified = x.LastModified,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted,
                BillThang = x.BillThang
            })
            .ToListAsync();
    }

}
