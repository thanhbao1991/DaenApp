using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
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
    public async Task<Result<List<ChiTieuHangNgayDto>>> CreateBulkAsync(
    ChiTieuHangNgayBulkCreateDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            return Result<List<ChiTieuHangNgayDto>>
                .Failure("Danh sách chi tiêu trống");

        var now = DateTime.Now;
        var result = new List<ChiTieuHangNgayDto>();

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in dto.Items)
            {
                if (item.NguyenLieuId == Guid.Empty || item.SoLuong <= 0)
                    continue;
                var nl = await _context.NguyenLieus
         .AsNoTracking()
         .FirstOrDefaultAsync(x => x.Id == item.NguyenLieuId && !x.IsDeleted);

                if (nl == null)
                    continue;

                var entity = new ChiTieuHangNgay
                {
                    Id = Guid.NewGuid(),
                    NguyenLieuId = item.NguyenLieuId,
                    SoLuong = item.SoLuong,
                    DonGia = item.DonGia,
                    ThanhTien = item.ThanhTien ?? item.SoLuong * item.DonGia,
                    GhiChu = item.GhiChu,
                    Ten = nl.Ten,
                    Ngay = dto.Ngay.Date,
                    NgayGio = dto.NgayGio ?? now,
                    BillThang = dto.BillThang,

                    LastModified = now,
                    IsDeleted = false
                };
                StringHelper.NormalizeAllStrings(entity);
                _context.ChiTieuHangNgays.Add(entity);

                // cập nhật kho
                await ApplyTonKhoDeltaAsync(
                    entity.NguyenLieuId,
                    entity.SoLuong,
                    entity.DonGia,
                    now
                );
                result.Add(ToDto(entity));
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        return Result<List<ChiTieuHangNgayDto>>
            .Success(result, $"Đã nhập {result.Count} dòng");
    }


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


            LastModified = e.LastModified,
            DeletedAt = e.DeletedAt,
            IsDeleted = e.IsDeleted
        };
    }

    private static bool IsNhapKho(ChiTieuHangNgay e)
    {
        return e.NguyenLieuId != Guid.Empty && e.SoLuong > 0;
    }



    //private async Task ApplyTonKhoDeltaAsync(
    //    ChiTieuHangNgay oldEntity,
    //    ChiTieuHangNgay newEntity,
    //    DateTime now)
    //{
    //    if (oldEntity.NguyenLieuId == Guid.Empty) return;

    //    var nl = await _context.NguyenLieus
    //        .Include(x => x.NguyenLieuBanHang)
    //        .FirstOrDefaultAsync(x => x.Id == oldEntity.NguyenLieuId && !x.IsDeleted);

    //    if (nl?.NguyenLieuBanHang == null) return;

    //    var heSo = nl.HeSoQuyDoiBanHang ?? 0;
    //    if (heSo <= 0) return;

    //    var oldQty = oldEntity.SoLuong * heSo;
    //    var newQty = newEntity.SoLuong * heSo;
    //    var delta = newQty - oldQty;

    //    if (delta == 0) return;

    //    nl.NguyenLieuBanHang.TonKho += delta;
    //    if (nl.NguyenLieuBanHang.TonKho < 0)
    //        nl.NguyenLieuBanHang.TonKho = 0;

    //    nl.NguyenLieuBanHang.LastModified = now;

    //    _context.NguyenLieuTransactions.Add(new NguyenLieuTransaction
    //    {
    //        Id = Guid.NewGuid(),
    //        NguyenLieuId = nl.NguyenLieuBanHang.Id,
    //        NgayGio = now,
    //        Loai = LoaiGiaoDichNguyenLieu.Nhap,
    //        SoLuong = delta,
    //        DonGia = newEntity.DonGia,
    //        GhiChu = $"Điều chỉnh từ ChiTieuHangNgay: {newEntity.Ten}",
    //        ChiTieuHangNgayId = newEntity.Id,
    //        LastModified = now,
    //        IsDeleted = false
    //    });
    //}
    private async Task ApplyTonKhoDeltaAsync(
    Guid nguyenLieuId,
    decimal deltaSoLuong,
    decimal donGia,
    DateTime now)
    {
        if (nguyenLieuId == Guid.Empty || deltaSoLuong == 0)
            return;

        var nl = await _context.NguyenLieus
            .Include(x => x.NguyenLieuBanHang)
            .FirstOrDefaultAsync(x => x.Id == nguyenLieuId && !x.IsDeleted);

        if (nl?.NguyenLieuBanHang == null) return;

        var heSo = nl.HeSoQuyDoiBanHang ?? 0;
        if (heSo <= 0) return;

        var delta = deltaSoLuong * heSo;

        nl.NguyenLieuBanHang.TonKho += delta;
        if (nl.NguyenLieuBanHang.TonKho < 0)
            nl.NguyenLieuBanHang.TonKho = 0;

        nl.NguyenLieuBanHang.LastModified = now;

        _context.NguyenLieuTransactions.Add(new NguyenLieuTransaction
        {
            Id = Guid.NewGuid(),
            NguyenLieuId = nl.NguyenLieuBanHang.Id,
            NgayGio = now,
            Loai = LoaiGiaoDichNguyenLieu.Nhap,
            SoLuong = delta,
            DonGia = donGia,
            GhiChu = "Điều chỉnh từ ChiTieuHangNgay",
            LastModified = now,
            IsDeleted = false
        });

        nl.GiaNhap = donGia;
        nl.LastModified = now;
    }

    public async Task<List<ChiTieuHangNgayDto>> GetAllAsync()
    {
        var today = DateTime.Today;
        var fromDate = today; // giống ChiTietHoaDonThanhToan

        return await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.Ngay >= fromDate)
            .OrderByDescending(x => x.Ngay)
            .ThenByDescending(x => x.LastModified)
            .Select(x => new ChiTieuHangNgayDto
            {
                Id = x.Id,
                Ten = x.Ten,
                GhiChu = x.GhiChu,

                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                ThanhTien = x.ThanhTien,

                Ngay = x.Ngay,
                NgayGio = x.NgayGio,

                NguyenLieuId = x.NguyenLieuId,
                BillThang = x.BillThang,


                LastModified = x.LastModified,
                DeletedAt = x.DeletedAt,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync();
    }
    public async Task<ChiTieuHangNgayDto?> GetByIdAsync(Guid id)
    {
        var e = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return e == null ? null : ToDto(e);
    }
    public async Task<Result<ChiTieuHangNgayDto>> UpdateAsync(Guid id, ChiTieuHangNgayDto dto)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure("Không tìm thấy chi tiêu.");

        //if (dto.LastModified < entity.LastModified)
        //  return Result<ChiTieuHangNgayDto>//.Failure("Dữ liệu đã được cập nhật ở nơi khác.");

        var now = DateTime.Now;

        var oldNguyenLieuId = entity.NguyenLieuId;
        var oldSoLuong = entity.SoLuong;
        var oldDonGia = entity.DonGia;

        var isChangeNguyenLieu = dto.NguyenLieuId != oldNguyenLieuId;

        // 1️⃣ Nếu đổi nguyên liệu → hoàn kho cũ
        if (isChangeNguyenLieu)
        {
            await ApplyTonKhoDeltaAsync(
                oldNguyenLieuId,
                -oldSoLuong,
                oldDonGia,
                now
            );
        }

        // 2️⃣ Update entity
        entity.NguyenLieuId = dto.NguyenLieuId;
        entity.SoLuong = dto.SoLuong;
        entity.DonGia = dto.DonGia;
        entity.ThanhTien = dto.ThanhTien > 0
            ? dto.ThanhTien
            : dto.SoLuong * dto.DonGia;
        entity.GhiChu = dto.GhiChu;
        entity.BillThang = dto.BillThang;
        entity.LastModified = now;

        // 3️⃣ Nhập kho
        if (isChangeNguyenLieu)
        {
            await ApplyTonKhoDeltaAsync(
                entity.NguyenLieuId,
                entity.SoLuong,
                entity.DonGia,
                now
            );
        }
        else
        {
            var delta = entity.SoLuong - oldSoLuong;

            if (delta != 0)
            {
                await ApplyTonKhoDeltaAsync(
                    entity.NguyenLieuId,
                    delta,
                    entity.DonGia,
                    now
                );
            }
        }

        await _context.SaveChangesAsync();

        return Result<ChiTieuHangNgayDto>
            .Success(ToDto(entity), "Đã cập nhật chi tiêu.")
            .WithId(id);
    }
    public async Task<Result<ChiTieuHangNgayDto>> CreateAsync(ChiTieuHangNgayDto dto)
    {
        if (dto.NguyenLieuId == Guid.Empty)
            return Result<ChiTieuHangNgayDto>.Failure("Vui lòng chọn nguyên liệu.");

        var now = DateTime.Now;

        var entity = new ChiTieuHangNgay
        {
            Id = Guid.NewGuid(),
            NguyenLieuId = dto.NguyenLieuId,
            Ten = dto.Ten,
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            ThanhTien = dto.ThanhTien > 0 ? dto.ThanhTien : dto.SoLuong * dto.DonGia,
            GhiChu = dto.GhiChu,
            BillThang = dto.BillThang,
            Ngay = dto.Ngay,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,
            LastModified = now,
            IsDeleted = false
        };

        _context.ChiTieuHangNgays.Add(entity);

        await ApplyTonKhoDeltaAsync(
       entity.NguyenLieuId,
       entity.SoLuong,
       entity.DonGia,
       now
   );

        await _context.SaveChangesAsync();

        return Result<ChiTieuHangNgayDto>.Success(ToDto(entity), "Đã thêm chi tiêu.")
            .WithId(entity.Id);
    }

    public async Task<Result<ChiTieuHangNgayDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure("Không tìm thấy chi tiêu.");

        var now = DateTime.Now;


        await ApplyTonKhoDeltaAsync(
     entity.NguyenLieuId,
     -entity.SoLuong,
     entity.DonGia,
     now);
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<ChiTieuHangNgayDto>.Success(ToDto(entity), "Đã xoá chi tiêu.")
            .WithId(id);
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


            await ApplyTonKhoDeltaAsync(
    entity.NguyenLieuId,
    entity.SoLuong,
    entity.DonGia,
    now);
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

