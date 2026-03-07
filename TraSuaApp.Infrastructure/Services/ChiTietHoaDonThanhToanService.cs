


using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Services;

public class ChiTietHoaDonThanhToanService : IChiTietHoaDonThanhToanService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTietHoaDonThanhToan"];

    public ChiTietHoaDonThanhToanService(AppDbContext context)
    {
        _context = context;
    }

    private static string GetGhiChu(decimal soTien, decimal soTienConLai)
        => soTien == soTienConLai ? "Thanh toán đủ" : "Thanh toán thiếu";

    private async Task SaveAndRecalcAsync(Guid hoaDonId)
    {
        await _context.SaveChangesAsync();
        await HoaDonHelper.RecalcConLaiAsync(_context, hoaDonId);
        await _context.SaveChangesAsync();
    }

    private static Expression<Func<ChiTietHoaDonThanhToan, ChiTietHoaDonThanhToanDto>> SelectDto()
    {
        return t => new ChiTietHoaDonThanhToanDto
        {
            Id = t.Id,
            Ten = t.KhachHangId != null ? t.KhachHang!.Ten : t.HoaDon!.TenBan,
            LoaiThanhToan = t.LoaiThanhToan,
            ChiTietHoaDonNoId = t.ChiTietHoaDonNoId,
            SoTien = t.SoTien,
            NgayGio = t.NgayGio,
            Ngay = t.Ngay,
            HoaDonId = t.HoaDonId,
            KhachHangId = t.KhachHangId,
            PhuongThucThanhToanId = t.PhuongThucThanhToanId,
            TenPhuongThucThanhToan = t.TenPhuongThucThanhToan,
            GhiChu = t.GhiChu,
            //        IsThanhToanHidden =
            //(
            //    (
            //       (t.PhuongThucThanhToanId == AppConstants.TienMatId && t.SoTien < t.HoaDon.ThanhTien)
            //    || (t.PhuongThucThanhToanId == AppConstants.BankN)
            //    || (t.PhuongThucThanhToanId == AppConstants.BankD)
            //    || (t.LoaiThanhToan == AppConstants.LoaiThanhToan_TNQN)
            //    || (t.LoaiThanhToan == AppConstants.LoaiThanhToan_TNTN)

            //    || (t.HoaDon.PhanLoai == AppConstants.TaiCho && t.HoaDon.KhachHangId != null)
            //    || (t.HoaDon.PhanLoai == AppConstants.Ship && t.HoaDon.NgayShip != null && t.HoaDon.NguoiShip == "Khánh")
            //    || (t.HoaDon.PhanLoai == AppConstants.MuaVe && t.HoaDon.KhachHangId != null)
            //    )
            //),
            CreatedAt = t.CreatedAt,
            DeletedAt = t.DeletedAt,
            IsDeleted = t.IsDeleted,
            LastModified = t.LastModified
        };
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> CreateAsync(ChiTietHoaDonThanhToanDto dto)
    {
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không được âm.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted)
            .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.ThanhTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure($"Số tiền còn lại cần thu: {soTienConLai:N0}.");

        var now = DateTime.Now;

        var entity = new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            SoTien = dto.SoTien,
            LoaiThanhToan = dto.LoaiThanhToan,
            ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,
            Ngay = dto.Ngay == default ? now.Date : dto.Ngay,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,
            PhuongThucThanhToanId = dto.PhuongThucThanhToanId,
            TenPhuongThucThanhToan = dto.TenPhuongThucThanhToan,
            GhiChu = GetGhiChu(dto.SoTien, soTienConLai),
            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        _context.ChiTietHoaDonThanhToans.Add(entity);

        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -dto.SoTien);
        await SaveAndRecalcAsync(entity.HoaDonId);

        var after = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == entity.Id)
            .Select(SelectDto())
            .FirstAsync();

        if (entity.ChiTietHoaDonNoId != null)
        {
            await DiscordService.SendAsync(
                DiscordEventType.TraNo,
                $"{entity.SoTien:N0}đ {dto.Ten}"
            );
        }

        return Result<ChiTietHoaDonThanhToanDto>
            .Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> UpdateAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure("Dữ liệu đã được cập nhật ở nơi khác.");

        var hoaDon = await _context.HoaDons.FindAsync(dto.HoaDonId);
        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId && !x.IsDeleted && x.Id != id)
            .SumAsync(x => x.SoTien);

        var soTienConLai = hoaDon.ThanhTien - tongDaThanhToan;

        if (dto.SoTien > soTienConLai)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure($"Số tiền còn lại cần thu: {soTienConLai:N0}.");

        var before = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(SelectDto())
            .FirstAsync();

        var oldHoaDonId = entity.HoaDonId;
        var oldSoTien = entity.SoTien;
        var oldNoId = entity.ChiTietHoaDonNoId;

        entity.SoTien = dto.SoTien;
        entity.ChiTietHoaDonNoId = dto.ChiTietHoaDonNoId;
        entity.LoaiThanhToan = dto.LoaiThanhToan;
        entity.KhachHangId = dto.KhachHangId;
        entity.HoaDonId = dto.HoaDonId;
        entity.PhuongThucThanhToanId = dto.PhuongThucThanhToanId;
        entity.TenPhuongThucThanhToan = dto.TenPhuongThucThanhToan;
        entity.GhiChu = GetGhiChu(dto.SoTien, soTienConLai);
        entity.LastModified = DateTime.Now;

        await NoHelper.UpdateSoTienConLaiAsync(_context, oldNoId, oldSoTien);
        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -dto.SoTien);

        await SaveAndRecalcAsync(entity.HoaDonId);

        if (oldHoaDonId != entity.HoaDonId)
            await HoaDonHelper.RecalcConLaiAsync(_context, oldHoaDonId);

        var after = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(SelectDto())
            .FirstAsync();

        return Result<ChiTietHoaDonThanhToanDto>
            .Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> DeleteAsync(Guid id)
    {
        return Result<ChiTietHoaDonThanhToanDto>
               .Failure($"Chức năng tạm thời bị khóa");

        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(SelectDto())
            .FirstAsync();

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, entity.SoTien);
        await SaveAndRecalcAsync(entity.HoaDonId);

        return Result<ChiTietHoaDonThanhToanDto>
            .Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<ChiTietHoaDonThanhToanDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>
                .Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await NoHelper.UpdateSoTienConLaiAsync(_context, entity.ChiTietHoaDonNoId, -entity.SoTien);
        await SaveAndRecalcAsync(entity.HoaDonId);

        var after = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(SelectDto())
            .FirstAsync();

        return Result<ChiTietHoaDonThanhToanDto>
            .Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetAllAsync()
    {
        var fromDate = DateTime.Today;

        return await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Ngay >= fromDate)
            .OrderByDescending(x => x.NgayGio)
            .Select(SelectDto())
            .ToListAsync();
    }

    public async Task<ChiTietHoaDonThanhToanDto?> GetByIdAsync(Guid id)
    {
        return await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(SelectDto())
            .FirstOrDefaultAsync();
    }

    public async Task<List<ChiTietHoaDonThanhToanDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(SelectDto())
            .ToListAsync();
    }
}