using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
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

    // =========================
    //  DTO mapping
    // =========================
    private static ChiTieuHangNgayDto ToDto(ChiTieuHangNgay e)
    {
        return new ChiTieuHangNgayDto
        {
            Id = e.Id,
            Ten = e.Ten,
            GhiChu = e.GhiChu,

            SoLuong = e.SoLuong,
            DonGia = e.DonGia,
            ThanhTien = e.ThanhTien,

            Ngay = e.Ngay,
            NgayGio = e.NgayGio,

            NguyenLieuId = e.NguyenLieuId,
            BillThang = e.BillThang,

            CreatedAt = e.CreatedAt,
            LastModified = e.LastModified,
            DeletedAt = e.DeletedAt,
            IsDeleted = e.IsDeleted
        };
    }

    // =========================
    //  Quy tắc: có phải phiếu nhập kho không?
    //  -> Chỉ cần có NguyenLieuId + SoLuong > 0
    // =========================
    private static bool IsNhapKho(ChiTieuHangNgay e)
    {
        return e.NguyenLieuId != Guid.Empty && e.SoLuong > 0;
    }

    // =========================
    //  Áp kho: sign = +1 (cộng), -1 (hoàn/trừ lại)
    //  - Cộng/trừ NguyenLieu.TonKho
    //  - Nếu NguyenLieu có NguyenLieuBanHangId + HeSoQuyDoiBanHang => cộng/trừ NguyenLieuBanHang.TonKho
    // =========================
    private async Task ApplyTonKhoNhapAsync(ChiTieuHangNgay e, int sign, DateTime now)
    {
        if (!IsNhapKho(e)) return;

        var nl = await _context.NguyenLieus
            .FirstOrDefaultAsync(x => x.Id == e.NguyenLieuId && !x.IsDeleted);

        if (nl == null) return;

        // 1) Kho nhập
        nl.LastModified = now;

        // 2) Kho bán hàng (nếu có map + hệ số)
        if (nl.NguyenLieuBanHangId != null && nl.NguyenLieuBanHangId != Guid.Empty)
        {
            var heSo = nl.HeSoQuyDoiBanHang ?? 0;
            if (heSo > 0)
            {
                var nlb = await _context.NguyenLieuBanHangs
                    .FirstOrDefaultAsync(x => x.Id == nl.NguyenLieuBanHangId && !x.IsDeleted);

                if (nlb != null)
                {
                    nlb.TonKho += (e.SoLuong * heSo) * sign;
                    if (nlb.TonKho < 0) nlb.TonKho = 0;
                    nlb.LastModified = now;
                }
            }
        }
    }

    // =========================
    //  CRUD
    // =========================
    public async Task<List<ChiTieuHangNgayDto>> GetAllAsync()
    {
        return await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified ?? x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<ChiTieuHangNgayDto?> GetByIdAsync(Guid id)
    {
        var e = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return e == null ? null : ToDto(e);
    }

    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto)
    {
        var now = DateTime.Now;

        if (dto.Id == Guid.Empty) dto.Id = Guid.NewGuid();

        // Validate tối thiểu cho dòng nhập kho (vì entity bắt buộc NguyenLieuId)
        if (dto.NguyenLieuId == Guid.Empty)
            return Result<ChiTieuHangNgayDto>.Failure("Vui lòng chọn nguyên liệu.");

        if (dto.SoLuong <= 0)
            return Result<ChiTieuHangNgayDto>.Failure("Số lượng phải > 0.");

        var entity = new ChiTieuHangNgay
        {
            Id = dto.Id,
            Ten = (dto.Ten ?? "").Trim(),
            GhiChu = dto.GhiChu,

            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,

            ThanhTien = dto.ThanhTien > 0 ? dto.ThanhTien : (dto.SoLuong * dto.DonGia),

            Ngay = dto.Ngay == default ? now.Date : dto.Ngay.Date,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,

            NguyenLieuId = dto.NguyenLieuId,
            BillThang = dto.BillThang,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false
        };

        StringHelper.NormalizeAllStrings(entity);

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.ChiTieuHangNgays.Add(entity);

            // ✅ Tăng kho ngay khi tạo phiếu nhập
            await ApplyTonKhoNhapAsync(entity, sign: +1, now);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

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

        if (dto.NguyenLieuId == Guid.Empty)
            return Result<ChiTieuHangNgayDto>.Failure("Vui lòng chọn nguyên liệu.");

        if (dto.SoLuong <= 0)
            return Result<ChiTieuHangNgayDto>.Failure("Số lượng phải > 0.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // ✅ Hoàn kho theo dữ liệu cũ
            await ApplyTonKhoNhapAsync(entity, sign: -1, now);

            // Update entity
            entity.Ten = (dto.Ten ?? "").Trim();
            entity.GhiChu = dto.GhiChu;

            entity.SoLuong = dto.SoLuong;
            entity.DonGia = dto.DonGia;
            entity.ThanhTien = dto.ThanhTien > 0 ? dto.ThanhTien : (dto.SoLuong * dto.DonGia);

            entity.Ngay = dto.Ngay == default ? entity.Ngay : dto.Ngay.Date;
            entity.NgayGio = dto.NgayGio == default ? entity.NgayGio : dto.NgayGio;

            entity.NguyenLieuId = dto.NguyenLieuId;
            entity.BillThang = dto.BillThang;

            entity.LastModified = now;
            StringHelper.NormalizeAllStrings(entity);

            // ✅ Áp kho theo dữ liệu mới
            await ApplyTonKhoNhapAsync(entity, sign: +1, now);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

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

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // ✅ Xoá phiếu nhập => hoàn kho
            await ApplyTonKhoNhapAsync(entity, sign: -1, now);

            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.LastModified = now;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

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

        var now = DateTime.Now;

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.LastModified = now;

            // ✅ Khôi phục phiếu nhập => cộng kho lại
            await ApplyTonKhoNhapAsync(entity, sign: +1, now);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        var after = ToDto(entity);
        return Result<ChiTieuHangNgayDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<ChiTieuHangNgayDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}