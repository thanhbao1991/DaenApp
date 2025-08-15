using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTietHoaDonThanhToanService : IChiTietHoaDonThanhToanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"];

    public ChiTietHoaDonThanhToanService(AppDbContext context)
    {
        _context = context;
    }

    private ChiTietHoaDonThanhToanDto ToDto(ChiTietHoaDonThanhToan entity)
    {
        return new ChiTietHoaDonThanhToanDto
        {
            Ten = entity.KhachHangId != null
                ? entity.KhachHang?.Ten
                : entity.HoaDon?.TenBan,

            Id = entity.Id,
            LoaiThanhToan = entity.LoaiThanhToan,
            ChiTietHoaDonNoId = entity.ChiTietHoaDonNoId,
            SoTien = entity.SoTien,
            NgayGio = entity.NgayGio,
            Ngay = entity.Ngay,
            HoaDonId = entity.HoaDonId,
            KhachHangId = entity.KhachHangId,
            PhuongThucThanhToanId = entity.PhuongThucThanhToanId,
            TenPhuongThucThanhToan = entity.PhuongThucThanhToan?.Ten,
            GhiChu = entity.GhiChu,

            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
            LastModified = entity.LastModified
        };
    }

    private async Task UpdateSoTienDaTraChiTietHoaDonNo(Guid? chiTietHoaDonNoId, decimal soTienDelta)
    {
        if (chiTietHoaDonNoId == null) return;

        var congNo = await _context.ChiTietHoaDonNos.FindAsync(chiTietHoaDonNoId.Value);
        if (congNo != null)
        {
            congNo.SoTienDaTra += soTienDelta;
            if (congNo.SoTienDaTra < 0) congNo.SoTienDaTra = 0;
            congNo.LastModified = DateTime.Now;
        }
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetAllAsync()
    {
        var list = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.Ngay >= DateTime.Today.AddDays(-1))
            .Include(x => x.HoaDon)
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<ChiTietHoaDonThanhToanDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto)
    {
        // Kiểm tra số tiền hợp lệ
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không được âm.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted)
            .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.TongTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Số tiền còn lại cần thu: {soTienConLai.ToString("N0")}.");

        var now = DateTime.Now;
        var entity = new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            SoTien = dto.SoTien,
            LoaiThanhToan = dto.LoaiThanhToan,
            ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId,
            NgayGio = now,
            Ngay = now.Date,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,
            PhuongThucThanhToanId = dto.PhuongThucThanhToanId,
            GhiChu = dto.GhiChu,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.ChiTietHoaDonThanhToans.Add(entity);

        await UpdateSoTienDaTraChiTietHoaDonNo(dto.ChiTietHoaDonNoId, dto.SoTien);

        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == entity.Id);

        var after = ToDto(afterEntity);
        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        // Kiểm tra số tiền hợp lệ
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không được âm.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted && x.Id != dto.Id)
            .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.TongTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Số tiền còn lại cần thu: {soTienConLai.ToString("N0")}.");


        var before = ToDto(entity);

        var oldSoTien = entity.SoTien;
        var oldChiTietHoaDonNoId = entity.ChiTietHoaDonNoId;

        entity.SoTien = dto.SoTien;
        entity.ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId;
        entity.LoaiThanhToan = dto.LoaiThanhToan;
        entity.KhachHangId = dto.KhachHangId;
        entity.HoaDonId = dto.HoaDonId;
        entity.PhuongThucThanhToanId = dto.PhuongThucThanhToanId;
        entity.GhiChu = dto.GhiChu;

        entity.LastModified = DateTime.Now;

        if (oldChiTietHoaDonNoId == dto.ChiTietHoaDonNoId)
        {
            var delta = dto.SoTien - oldSoTien;
            await UpdateSoTienDaTraChiTietHoaDonNo(dto.ChiTietHoaDonNoId, delta);
        }
        else
        {
            await UpdateSoTienDaTraChiTietHoaDonNo(oldChiTietHoaDonNoId, -oldSoTien);
            await UpdateSoTienDaTraChiTietHoaDonNo(dto.ChiTietHoaDonNoId, dto.SoTien);
        }

        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(afterEntity);
        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await UpdateSoTienDaTraChiTietHoaDonNo(entity.ChiTietHoaDonNoId, -entity.SoTien);

        await _context.SaveChangesAsync();

        return Result<ChiTietHoaDonThanhToanDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var afterEntity = await _context.ChiTietHoaDonThanhToans
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .FirstAsync(x => x.Id == id);

        var after = ToDto(afterEntity);
        return Result<ChiTietHoaDonThanhToanDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.ChiTietHoaDonThanhToans.AsNoTracking()
            .Include(x => x.KhachHang)
            .Include(x => x.PhuongThucThanhToan)
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}