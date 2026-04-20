using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChiTieuHangNgayController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;

    public ChiTieuHangNgayController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static ChiTieuHangNgayDto ToDto(ChiTieuHangNgay e)
    {
        return new ChiTieuHangNgayDto
        {
            Id = e.Id,
            Ten = e.Ten,
            SoLuong = e.SoLuong,
            DonGia = e.DonGia,
            ThanhTien = e.ThanhTien,
            Ngay = e.Ngay,
            NgayGio = e.NgayGio,
            NguyenLieuId = e.NguyenLieuId,
            BillThang = e.BillThang,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "ChiTieuHangNgay", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "ChiTieuHangNgay", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    [HttpGet]
    public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> GetAll(DateTime? ngay)
    {
        var targetDate = ngay?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);

        var list = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .Where(x => x.NgayGio >= targetDate &&
                        x.NgayGio < nextDate)
            .OrderByDescending(x => x.NgayGio)
            .Select(x => new ChiTieuHangNgayDto
            {
                Id = x.Id,
                Ten = x.Ten,
                SoLuong = x.SoLuong,
                DonGia = x.DonGia,
                ThanhTien = x.ThanhTien,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                NguyenLieuId = x.NguyenLieuId,
                BillThang = x.BillThang,
                LastModified = x.LastModified
            })
            .ToListAsync();

        return Result<List<ChiTieuHangNgayDto>>.Success(list);
    }
    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> GetById(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure("Không tìm thấy.");

        return Result<ChiTieuHangNgayDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Create(ChiTieuHangNgayDto dto)
    {
        var now = DateTime.Now;

        var nl = await _context.NguyenLieus
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == dto.NguyenLieuId);

        if (nl == null)
            return Result<ChiTieuHangNgayDto>.Failure("Nguyên liệu không tồn tại.");

        var entity = new ChiTieuHangNgay
        {
            Id = Guid.NewGuid(),
            Ten = nl.Ten, // 🟟 FIX
            SoLuong = dto.SoLuong,
            DonGia = dto.DonGia,
            ThanhTien = dto.ThanhTien > 0 ? dto.ThanhTien : dto.SoLuong * dto.DonGia,
            NguyenLieuId = dto.NguyenLieuId,
            Ngay = dto.Ngay,
            NgayGio = dto.NgayGio == default ? now : dto.NgayGio,
            BillThang = dto.BillThang,
            LastModified = now
        };

        _context.ChiTieuHangNgays.Add(entity);

        await ApplyTonKhoDeltaAsync(entity.NguyenLieuId, entity.SoLuong, entity.DonGia, now);

        await _context.SaveChangesAsync();

        await Notify("created", entity.Id);

        return Result<ChiTieuHangNgayDto>.Success(ToDto(entity), "Đã thêm.")
            ;
    }

    // ======================
    // BULK CREATE
    // ======================
    [HttpPost("bulk")]
    public async Task<ActionResult<Result<List<ChiTieuHangNgayDto>>>> CreateBulk(ChiTieuHangNgayBulkCreateDto dto)
    {
        var now = DateTime.Now;
        var result = new List<ChiTieuHangNgayDto>();

        foreach (var item in dto.Items)
        {
            if (item.NguyenLieuId == Guid.Empty || item.SoLuong <= 0)
                continue;

            var nl = await _context.NguyenLieus
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == item.NguyenLieuId);

            if (nl == null) continue;

            var entity = new ChiTieuHangNgay
            {
                Id = Guid.NewGuid(),
                Ten = nl.Ten, // 🟟 FIX
                SoLuong = item.SoLuong,
                DonGia = item.DonGia,
                ThanhTien = item.ThanhTien ?? item.SoLuong * item.DonGia,
                NguyenLieuId = item.NguyenLieuId,
                Ngay = dto.Ngay,
                NgayGio = dto.NgayGio ?? now,
                BillThang = dto.BillThang,
                LastModified = now
            };

            _context.ChiTieuHangNgays.Add(entity);

            await ApplyTonKhoDeltaAsync(entity.NguyenLieuId, entity.SoLuong, entity.DonGia, now);

            result.Add(ToDto(entity));
        }

        await _context.SaveChangesAsync();

        return Result<List<ChiTieuHangNgayDto>>.Success(result);
    }

    // ======================
    // UPDATE
    // ======================
    [HttpPut("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Update(Guid id, ChiTieuHangNgayDto dto)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure("Không tìm thấy.");

        var now = DateTime.Now;

        await ApplyTonKhoDeltaAsync(entity.NguyenLieuId, -entity.SoLuong, entity.DonGia, now);

        entity.BillThang = dto.BillThang;
        entity.SoLuong = dto.SoLuong;
        entity.DonGia = dto.DonGia;
        entity.ThanhTien = dto.SoLuong * dto.DonGia;
        entity.LastModified = now;

        await ApplyTonKhoDeltaAsync(entity.NguyenLieuId, entity.SoLuong, entity.DonGia, now);

        await _context.SaveChangesAsync();

        await Notify("updated", id);

        return Result<ChiTieuHangNgayDto>.Success(ToDto(entity), "Cập nhật thành công.")
            ;
    }

    // ======================
    // DELETE
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTieuHangNgayDto>>> Delete(Guid id)
    {
        var entity = await _context.ChiTieuHangNgays
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTieuHangNgayDto>.Failure("Không tìm thấy.");

        var now = DateTime.Now;

        await ApplyTonKhoDeltaAsync(entity.NguyenLieuId, -entity.SoLuong, entity.DonGia, now);

        _context.ChiTieuHangNgays.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<ChiTieuHangNgayDto>.Success(null, "Đã xoá.")
            ;
    }

    // ======================
    // KHO
    // ======================
    private async Task ApplyTonKhoDeltaAsync(Guid nguyenLieuId, decimal soLuong, decimal donGia, DateTime now)
    {
        var nl = await _context.NguyenLieus
            .Include(x => x.NguyenLieuBanHang)
            .FirstOrDefaultAsync(x => x.Id == nguyenLieuId);

        if (nl?.NguyenLieuBanHang == null) return;

        var heSo = nl.HeSoQuyDoiBanHang ?? 0;
        if (heSo <= 0) return;

        var delta = soLuong * heSo;

        nl.NguyenLieuBanHang.TonKho += delta;
        nl.NguyenLieuBanHang.LastModified = now;

        nl.GiaNhap = donGia;
        nl.LastModified = now;
    }
}