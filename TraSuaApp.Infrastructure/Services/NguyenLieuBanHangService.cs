using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

public class NguyenLieuBanHangService : INguyenLieuBanHangService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuBanHang"];

    public NguyenLieuBanHangService(AppDbContext context)
    {
        _context = context;
    }

    private static NguyenLieuBanHangDto ToDto(NguyenLieuBanHang entity)
    {
        return new NguyenLieuBanHangDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            DangSuDung = entity.DangSuDung,

            DonViTinh = entity.DonViTinh,
            TonKho = entity.TonKho,

            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            DeletedAt = entity.DeletedAt,
            IsDeleted = entity.IsDeleted,
        };
    }

    public async Task<List<NguyenLieuBanHangDto>> GetAllAsync()
    {
        return await _context.NguyenLieuBanHangs.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }

    public async Task<NguyenLieuBanHangDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NguyenLieuBanHangs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }

    public async Task<Result<NguyenLieuBanHangDto>> CreateAsync(NguyenLieuBanHangDto dto)
    {
        bool daTonTai = await _context.NguyenLieuBanHangs
            .AnyAsync(x => x.Ten.ToLower() == dto.Ten.ToLower() && !x.IsDeleted);

        if (daTonTai)
            return Result<NguyenLieuBanHangDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var now = DateTime.Now;

        // ✅ TonKho không âm
        var tonKho = dto.TonKho;
        if (tonKho < 0) tonKho = 0;

        var entity = new NguyenLieuBanHang
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten.Trim(),
            DangSuDung = dto.DangSuDung,

            DonViTinh = dto.DonViTinh,
            TonKho = tonKho,

            CreatedAt = now,
            LastModified = now,
            IsDeleted = false,
        };

        _context.NguyenLieuBanHangs.Add(entity);

        // ✅ Nếu tạo mới mà nhập TonKho khác 0 -> log 1 giao dịch điều chỉnh khởi tạo
        if (entity.TonKho != 0)
        {
            _context.NguyenLieuTransactions.Add(new NguyenLieuTransaction
            {
                Id = Guid.NewGuid(),
                NguyenLieuId = entity.Id,
                NgayGio = now,
                Loai = LoaiGiaoDichNguyenLieu.DieuChinh,
                SoLuong = entity.TonKho, // từ 0 lên TonKho
                DonGia = null,
                GhiChu = "Khởi tạo tồn kho khi tạo nguyên liệu bán hàng",
                HoaDonId = null,
                ChiTieuHangNgayId = null,
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuBanHangDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuBanHangDto>> UpdateAsync(Guid id, NguyenLieuBanHangDto dto)
    {
        var entity = await _context.NguyenLieuBanHangs
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<NguyenLieuBanHangDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (dto.LastModified < entity.LastModified)
            return Result<NguyenLieuBanHangDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        bool daTonTai = await _context.NguyenLieuBanHangs
            .AnyAsync(x => x.Id != id &&
                           x.Ten.ToLower() == dto.Ten.ToLower() &&
                           !x.IsDeleted);

        if (daTonTai)
            return Result<NguyenLieuBanHangDto>.Failure($"{_friendlyName} {dto.Ten} đã tồn tại.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        // ✅ giữ lại tồn cũ để log delta
        var oldTonKho = entity.TonKho;

        // Cập nhật
        entity.Ten = dto.Ten.Trim();
        entity.DangSuDung = dto.DangSuDung;

        entity.DonViTinh = dto.DonViTinh;

        // ✅ TonKho không âm
        var newTonKho = dto.TonKho;
        if (newTonKho < 0) newTonKho = 0;

        entity.TonKho = newTonKho;
        entity.LastModified = now;

        // ✅ LOG TRANSACTION nếu có thay đổi tồn
        var delta = newTonKho - oldTonKho;
        if (delta != 0)
        {
            _context.NguyenLieuTransactions.Add(new NguyenLieuTransaction
            {
                Id = Guid.NewGuid(),
                NguyenLieuId = entity.Id,
                NgayGio = now,
                Loai = LoaiGiaoDichNguyenLieu.DieuChinh,
                SoLuong = delta, // +/- theo delta
                DonGia = null,
                GhiChu = "Điều chỉnh tồn kho thủ công từ NguyenLieuBanHangEdit",
                HoaDonId = null,
                ChiTieuHangNgayId = null,
                CreatedAt = now,
                LastModified = now,
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuBanHangDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<NguyenLieuBanHangDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.NguyenLieuBanHangs.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<NguyenLieuBanHangDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        var before = ToDto(entity);
        var now = DateTime.Now;

        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.LastModified = now;

        await _context.SaveChangesAsync();

        return Result<NguyenLieuBanHangDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<NguyenLieuBanHangDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.NguyenLieuBanHangs.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<NguyenLieuBanHangDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

        if (!entity.IsDeleted)
            return Result<NguyenLieuBanHangDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.LastModified = DateTime.Now;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<NguyenLieuBanHangDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<NguyenLieuBanHangDto>> GetUpdatedSince(DateTime lastSync)
    {
        return await _context.NguyenLieuBanHangs.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .Select(x => ToDto(x))
            .ToListAsync();
    }
}
