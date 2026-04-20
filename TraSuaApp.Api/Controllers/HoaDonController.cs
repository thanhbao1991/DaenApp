using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class HoaDonController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["HoaDon"];

    private const string SignalCreate = "CREATE";
    private const string SignalUpdate = "UPDATE";
    private const string SignalDelete = "DEL";
    private const string SignalF1 = "F1";
    private const string SignalF4 = "F4";
    private const string SignalF12 = "F12";
    private const string SignalEsc = "ESC";
    private const string SignalRollback = "ROLLBACK";
    private const string SignalPrint = "PRINT";

    private const string HeaderConnectionId = "X-Connection-Id";
    private const string HeaderSignalRConnectionId = "X-SignalR-ConnectionId";

    public HoaDonController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private string? GetSenderConnectionId()
    {
        var fromHeader1 = Request.Headers[HeaderSignalRConnectionId].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromHeader1))
            return fromHeader1;

        var fromHeader2 = Request.Headers[HeaderConnectionId].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(fromHeader2))
            return fromHeader2;

        return null;
    }

    private Task NotifyAsync(string signal, Guid id)
    {
        var senderConnectionId = GetSenderConnectionId();

        var payload = new SignalMessageDto
        {
            Id = id.ToString(),
            SenderConnectionId = senderConnectionId
        };

        if (!string.IsNullOrWhiteSpace(senderConnectionId))
        {
            return _hub.Clients
                .AllExcept(new[] { senderConnectionId })
                .SendAsync(signal, payload);
        }

        return _hub.Clients.All.SendAsync(signal, payload);
    }

    private static async Task SafeRollbackAsync(IDbContextTransaction tx)
    {
        try
        {
            var dbTx = tx.GetDbTransaction();
            if (dbTx.Connection != null)
                await tx.RollbackAsync();
        }
        catch
        {
            // transaction đã commit/dispose hoặc đã rollback trước đó
        }
    }

    private static async Task LogAdminAsync(Exception ex)
    {
        try
        {
            await DiscordService.SendAsync(DiscordEventType.Admin, ex.ToString());
        }
        catch
        {
            // không làm hỏng luồng chính
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<HoaDonDto>>>> GetAll(DateTime? ngay)
    {
        var targetDate = ngay?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);

        var list = await (
            from h in _context.HoaDons.AsNoTracking()
            where h.Ngay >= targetDate && h.Ngay < nextDate
            orderby h.LastModified descending
            select new HoaDonDto
            {
                Id = h.Id,
                MaHoaDon = h.MaHoaDon,
                Ngay = h.Ngay,
                NgayGio = h.NgayGio,
                NgayShip = h.NgayShip,
                NguoiShip = h.NguoiShip,

                NgayNo = h.NgayNo,
                NgayIn = h.NgayIn,
                PhanLoai = h.PhanLoai,
                TenBan = h.TenBan,
                TenKhachHangText = h.TenKhachHangText,
                DiaChiText = h.DiaChiText,
                SoDienThoaiText = h.SoDienThoaiText,
                VoucherId = h.VoucherId,
                KhachHangId = h.KhachHangId,
                TongTien = h.TongTien,
                GiamGia = h.GiamGia,
                ThanhTien = h.ThanhTien,
                GhiChu = h.GhiChu,
                GhiChuShipper = h.GhiChuShipper,

                LastModified = h.LastModified
            }
        ).ToListAsync();

        return Result<List<HoaDonDto>>.Success(list);
    }

    [HttpPost]
    public async Task<ActionResult<Result<HoaDonDto>>> Create([FromBody] HoaDonDto dto)
    {
        var result = await CreateAsync(dto);

        if (result.IsSuccess && result.Data != null)
            await NotifyAsync(SignalCreate, result.Data.Id);

        return result;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> Update(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdateAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalUpdate, id);

        return result;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> Delete(Guid id)
    {
        var result = await DeleteAsync(id);

        if (result.IsSuccess)
            await NotifyAsync(SignalDelete, id);

        return result;
    }

    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<HoaDonDto>>> UpdateSingle(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdateSingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalUpdate, id);

        return result;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<HoaDonDto>>> GetById(Guid id)
    {
        var dto = await GetByIdAsync(id);
        if (dto == null)
            return Result<HoaDonDto>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<HoaDonDto>.Success(dto);
    }

    [HttpGet("get-khach-hang-info/{khachHangId}")]
    public async Task<ActionResult<Result<KhachHangInfoDto>>> GetKhachHangInfo(Guid khachHangId)
    {
        var info = await GetKhachHangInfoAsync(khachHangId);

        if (info == null)
            return Result<KhachHangInfoDto>.Failure("Không tìm thấy khách hàng.");

        return Result<KhachHangInfoDto>.Success(info);
    }

    [HttpPut("{id}/f1")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdateF1Single(Guid id, [FromBody] ChiTietHoaDonThanhToanDto dto)
    {
        var result = await UpdateF1F4SingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalF1, id);

        return result;
    }

    [HttpPut("{id}/f4")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdateF4Single(Guid id, [FromBody] ChiTietHoaDonThanhToanDto dto)
    {
        var result = await UpdateF1F4SingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalF4, id);

        return result;
    }

    [HttpPut("{id}/f12")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdateF12Single(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdateF12SingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalF12, id);

        return result;
    }

    [HttpPut("{id}/esc")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdateEscSingle(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdateEscSingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalEsc, id);

        return result;
    }

    [HttpPut("{id}/rollback")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdateRollBackSingle(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdateRollBackSingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalRollback, id);

        return result;
    }

    [HttpPut("{id}/print")]
    public async Task<ActionResult<Result<HoaDonNoDto>>> UpdatePrintSingle(Guid id, [FromBody] HoaDonDto dto)
    {
        var result = await UpdatePrintSingleAsync(id, dto);

        if (result.IsSuccess)
            await NotifyAsync(SignalPrint, id);

        return result;
    }

    private HoaDonDto ToDto(HoaDon entity)
    {
        return new HoaDonDto
        {
            Id = entity.Id,
            MaHoaDon = entity.MaHoaDon,
            Ngay = entity.Ngay,
            NgayGio = entity.NgayGio,
            NgayShip = entity.NgayShip,
            NguoiShip = entity.NguoiShip,

            NgayNo = entity.NgayNo,
            NgayIn = entity.NgayIn,
            PhanLoai = entity.PhanLoai,
            TenBan = entity.TenBan,
            TenKhachHangText = !string.IsNullOrWhiteSpace(entity.TenKhachHangText) ? entity.TenKhachHangText : entity.TenBan,
            DiaChiText = entity.DiaChiText,
            SoDienThoaiText = entity.SoDienThoaiText,
            VoucherId = entity.VoucherId,
            KhachHangId = entity.KhachHangId,
            TongTien = entity.TongTien,
            GiamGia = entity.GiamGia,
            ThanhTien = entity.ThanhTien,
            GhiChu = entity.GhiChu,
            GhiChuShipper = entity.GhiChuShipper,

            LastModified = entity.LastModified,
        };
    }

    private async Task<KhachHang?> GetOrCreateKhachHangAsync(HoaDonDto dto, DateTime now)
    {
        var phoneRaw = dto.SoDienThoaiText ?? string.Empty;
        var addrRaw = dto.DiaChiText ?? string.Empty;
        var nameRaw = dto.TenKhachHangText ?? string.Empty;

        var phone = phoneRaw.Trim();
        var addr = addrRaw.Trim();
        var name = nameRaw.Trim();

        if (dto.KhachHangId == null &&
            string.IsNullOrWhiteSpace(phone) &&
            (string.IsNullOrWhiteSpace(name) ||
             name.Equals("Khách lẻ", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        KhachHang? kh = null;
        if (dto.KhachHangId != null)
        {
            kh = await _context.KhachHangs
                .FirstOrDefaultAsync(x => x.Id == dto.KhachHangId.Value);
        }

        if (kh == null && !string.IsNullOrWhiteSpace(phone))
        {
            var khId = await _context.KhachHangPhones.AsNoTracking()
                        .Where(p => p.SoDienThoai == phone)
                        .Select(p => (Guid?)p.KhachHangId)
                        .FirstOrDefaultAsync();

            if (khId != null)
            {
                kh = await _context.KhachHangs
                    .FirstOrDefaultAsync(x => x.Id == khId.Value);
            }
        }

        var cleanedName = string.IsNullOrWhiteSpace(name) ? null : name;

        if (kh == null && (!string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(cleanedName)))
        {
            kh = new KhachHang
            {
                Id = Guid.NewGuid(),
                Ten = string.IsNullOrWhiteSpace(cleanedName) ? "Khách lẻ" : cleanedName,
                DuocNhanVoucher = true,
                LastModified = now,
            };

            StringHelper.NormalizeAllStrings(kh);
            _context.KhachHangs.Add(kh);

            var phonesDto = new List<KhachHangPhoneDto>();
            var addrsDto = new List<KhachHangAddressDto>();

            if (!string.IsNullOrWhiteSpace(phone))
            {
                _context.KhachHangPhones.Add(new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    SoDienThoai = phone,
                    IsDefault = true
                });
                phonesDto.Add(new KhachHangPhoneDto { SoDienThoai = phone, IsDefault = true });
            }

            if (!string.IsNullOrWhiteSpace(addr))
            {
                var capAddr = StringHelper.CapitalizeEachWord(addr);
                _context.KhachHangAddresses.Add(new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    DiaChi = capAddr,
                    IsDefault = true
                });
                addrsDto.Add(new KhachHangAddressDto { DiaChi = capAddr, IsDefault = true });
            }

            var dtoTmp = new KhachHangDto { Ten = kh.Ten, Phones = phonesDto, Addresses = addrsDto };
            kh.TimKiem = KhachHangSearchHelper.BuildTimKiem(dtoTmp);

            return kh;
        }

        bool changed = false;

        if (kh != null && !string.IsNullOrWhiteSpace(phone))
        {
            var hasPhone = await _context.KhachHangPhones
                .AnyAsync(p => p.KhachHangId == kh.Id && p.SoDienThoai == phone);

            if (!hasPhone)
            {
                _context.KhachHangPhones.Add(new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    SoDienThoai = phone,
                    IsDefault = false
                });
                changed = true;
            }
        }

        if (kh != null && !string.IsNullOrWhiteSpace(addr))
        {
            var capAddr = StringHelper.CapitalizeEachWord(addr);
            var hasAddr = await _context.KhachHangAddresses
                .AnyAsync(a => a.KhachHangId == kh.Id && a.DiaChi.ToLower() == capAddr.ToLower());

            if (!hasAddr)
            {
                _context.KhachHangAddresses.Add(new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    KhachHangId = kh.Id,
                    DiaChi = capAddr,
                    IsDefault = false
                });
                changed = true;
            }
        }

        if (kh != null && changed)
        {
            var phonesCur = await _context.KhachHangPhones
                .Where(p => p.KhachHangId == kh.Id)
                .OrderByDescending(p => p.IsDefault)
                .Select(p => new KhachHangPhoneDto { SoDienThoai = p.SoDienThoai, IsDefault = p.IsDefault })
                .ToListAsync();

            var addrsCur = await _context.KhachHangAddresses
                .Where(a => a.KhachHangId == kh.Id)
                .OrderByDescending(a => a.IsDefault)
                .Select(a => new KhachHangAddressDto { DiaChi = a.DiaChi, IsDefault = a.IsDefault })
                .ToListAsync();

            var dtoTmp = new KhachHangDto { Ten = kh.Ten, Phones = phonesCur, Addresses = addrsCur };
            kh.TimKiem = KhachHangSearchHelper.BuildTimKiem(dtoTmp);
            kh.LastModified = now;
        }

        return kh;
    }

    private async Task<(decimal tongTien, decimal giamGia, decimal thanhTien)> AddChiTietAsync(Guid hoaDonId, HoaDonDto dto, DateTime now)
    {
        decimal tongTien = 0;

        var toppingLookup = dto.ChiTietHoaDonToppings
            .GroupBy(tp => tp.ChiTietHoaDonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        int autoStt = 1;

        foreach (var ct in dto.ChiTietHoaDons)
        {
            var bienThe = await _context.SanPhamBienThes
                .Include(bt => bt.SanPham)
                .FirstOrDefaultAsync(bt => bt.Id == ct.SanPhamBienTheId);

            decimal giaMacDinh = bienThe?.GiaBan ?? 0;
            decimal donGia = ct.DonGia > 0 ? ct.DonGia : giaMacDinh;
            decimal thanhTienSP = donGia * ct.SoLuong;

            if (dto.KhachHangId != null && bienThe != null && donGia != giaMacDinh)
            {
                var existingCustom = await _context.KhachHangGiaBans
                    .FirstOrDefaultAsync(x =>
                        x.KhachHangId == dto.KhachHangId.Value &&
                        x.SanPhamBienTheId == bienThe.Id
                        );

                if (existingCustom == null)
                {
                    var newCustom = new KhachHangGiaBan
                    {
                        Id = Guid.NewGuid(),
                        KhachHangId = dto.KhachHangId.Value,
                        SanPhamBienTheId = bienThe.Id,
                        GiaBan = donGia,
                        LastModified = now,
                    };
                    _context.KhachHangGiaBans.Add(newCustom);
                }
                else if (existingCustom.GiaBan != donGia)
                {
                    existingCustom.GiaBan = donGia;
                    existingCustom.LastModified = now;
                }
            }

            decimal tienToppingSP = 0;
            Guid chiTietId = Guid.NewGuid();

            if (toppingLookup.TryGetValue(ct.Id, out var tpList))
            {
                foreach (var tp in tpList)
                {
                    var topping = await _context.Toppings.FirstOrDefaultAsync(x => x.Id == tp.ToppingId);
                    decimal giaTopping = topping?.Gia ?? tp.Gia;
                    decimal tienTP = giaTopping * tp.SoLuong;
                    tienToppingSP += tienTP;

                    _context.ChiTietHoaDonToppings.Add(new ChiTietHoaDonTopping
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDonId,
                        ChiTietHoaDonId = chiTietId,
                        TenTopping = tp.Ten,
                        ToppingId = tp.ToppingId,
                        SoLuong = tp.SoLuong,
                        Gia = giaTopping,
                        LastModified = now,
                    });
                }
            }

            tongTien += thanhTienSP + tienToppingSP;

            int stt = ct.Stt > 0 ? ct.Stt : autoStt++;

            _context.ChiTietHoaDons.Add(new ChiTietHoaDonEntity
            {
                Stt = stt,
                Id = chiTietId,
                HoaDonId = hoaDonId,
                SanPhamBienTheId = ct.SanPhamBienTheId,
                SanPhamId = bienThe?.SanPhamId ?? Guid.Empty,

                SoLuong = ct.SoLuong,
                DonGia = donGia,
                ThanhTien = thanhTienSP + tienToppingSP,

                TenSanPham = bienThe?.SanPham?.Ten ?? string.Empty,
                TenBienThe = bienThe?.TenBienThe ?? string.Empty,
                ToppingText = ct.ToppingText ?? "",
                NoteText = ct.NoteText,

                LastModified = now,
            });
        }

        decimal giamGia = 0;

        if (dto.VoucherId != null && dto.VoucherId != Guid.Empty)
        {
            var voucher = await _context.Vouchers.FindAsync(dto.VoucherId);
            if (voucher != null)
            {
                giamGia = DiscountHelper.TinhGiamGia(
                    tongTien,
                    voucher.KieuGiam,
                    voucher.GiaTri,
                    lamTron: true
                );
            }
        }

        if (giamGia > tongTien) giamGia = tongTien;
        decimal thanhTien = tongTien - giamGia;

        return (tongTien, giamGia, thanhTien);
    }

    private static IEnumerable<(Guid BienTheId, decimal SoLuongSP)> ExtractBienTheFromDto(HoaDonDto dto)
    {
        return (dto.ChiTietHoaDons ?? new ObservableCollection<ChiTietHoaDonDto>())
            .Where(x => x.SanPhamBienTheId != Guid.Empty && x.SoLuong > 0)
            .Select(x => (BienTheId: x.SanPhamBienTheId, SoLuongSP: (decimal)x.SoLuong));
    }

    private static IEnumerable<(Guid BienTheId, decimal SoLuongSP)> ExtractBienTheFromEntity(IEnumerable<ChiTietHoaDonEntity> cts)
    {
        return cts
            .Where(x => x.SanPhamBienTheId != Guid.Empty && x.SoLuong > 0)
            .Select(x => (BienTheId: x.SanPhamBienTheId, SoLuongSP: (decimal)x.SoLuong));
    }

    private async Task ApplyTonKhoByCongThucAsync(
        IEnumerable<(Guid BienTheId, decimal SoLuongSP)> chiTietBienThe,
        int sign,
        DateTime now,
        Guid? hoaDonId)
    {
        if (sign != 1 && sign != -1)
            throw new ArgumentException("sign phải là +1 hoặc -1");

        var list = chiTietBienThe
            .Where(x => x.BienTheId != Guid.Empty && x.SoLuongSP > 0)
            .ToList();

        if (!list.Any()) return;

        var qtyByBienThe = list
            .GroupBy(x => x.BienTheId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuongSP));

        var bienTheIds = qtyByBienThe.Keys.ToList();

        var congThucPick = await _context.CongThucs
            .AsNoTracking()
            .Where(ct => bienTheIds.Contains(ct.SanPhamBienTheId))
            .OrderByDescending(ct => ct.IsDefault)
            .ThenByDescending(ct => ct.LastModified)
            .Select(ct => new { ct.Id, ct.SanPhamBienTheId })
            .ToListAsync();

        if (!congThucPick.Any()) return;

        var congThucIdByBienThe = congThucPick
            .GroupBy(x => x.SanPhamBienTheId)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var congThucIds = congThucIdByBienThe.Values.ToList();

        var suDung = await _context.SuDungNguyenLieus
            .AsNoTracking()
            .Where(x => congThucIds.Contains(x.CongThucId))
            .Select(x => new
            {
                x.CongThucId,
                NguyenLieuBanHangId = x.NguyenLieuId,
                DinhMuc = x.SoLuong
            })
            .ToListAsync();

        if (!suDung.Any()) return;

        var suDungLookup = suDung
            .GroupBy(x => x.CongThucId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var tongTheoNLBH = new Dictionary<Guid, decimal>();

        foreach (var btId in bienTheIds)
        {
            if (!congThucIdByBienThe.TryGetValue(btId, out var congThucId)) continue;
            if (!qtyByBienThe.TryGetValue(btId, out var soLuongSP)) continue;
            if (!suDungLookup.TryGetValue(congThucId, out var items)) continue;

            foreach (var it in items)
            {
                if (it.NguyenLieuBanHangId == Guid.Empty) continue;
                if (it.DinhMuc <= 0) continue;

                var soLuongCan = it.DinhMuc * soLuongSP;

                if (!tongTheoNLBH.ContainsKey(it.NguyenLieuBanHangId))
                    tongTheoNLBH[it.NguyenLieuBanHangId] = 0;

                tongTheoNLBH[it.NguyenLieuBanHangId] += soLuongCan;
            }
        }

        if (!tongTheoNLBH.Any()) return;

        var ids = tongTheoNLBH.Keys.ToList();

        var nlbhs = await _context.NguyenLieuBanHangs
            .Where(x => ids.Contains(x.Id))
            .ToListAsync();

        foreach (var nlb in nlbhs)
        {
            if (!tongTheoNLBH.TryGetValue(nlb.Id, out var delta)) continue;

            nlb.TonKho += sign * delta;
            nlb.LastModified = now;
        }
    }

    public async Task<List<HoaDonDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.HoaDons.AsNoTracking()
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<List<HoaDonDto>> GetAllAsync()
    {
        var today = DateTime.Today;
        var fromDate = today.AddDays(-1);

        var list = await (
            from h in _context.HoaDons.AsNoTracking()
            join v in _context.HoaDonNos
                on h.Id equals v.Id into hv
            from v in hv.DefaultIfEmpty()
            where h.Ngay >= fromDate
            orderby h.LastModified descending
            select new
            {
                h.Id,
                h.MaHoaDon,
                h.Ngay,
                h.NgayGio,
                h.NgayShip,
                h.NguoiShip,

                h.NgayNo,
                h.NgayIn,
                h.PhanLoai,
                h.TenBan,
                h.TenKhachHangText,
                h.DiaChiText,
                h.SoDienThoaiText,
                h.VoucherId,
                h.KhachHangId,
                h.TongTien,
                h.GiamGia,
                h.ThanhTien,
                h.GhiChu,
                h.GhiChuShipper,

                h.LastModified,
                CoTienMat = _context.ChiTietHoaDonThanhToans.Any(t =>
                           t.HoaDonId == h.Id &&
                            t.PhuongThucThanhToanId == AppConstants.TienMatId),

                CoChuyenKhoan = _context.ChiTietHoaDonThanhToans.Any(t =>
                        t.HoaDonId == h.Id &&
                   t.PhuongThucThanhToanId == AppConstants.ChuyenKhoanId),

                IsThanhToanHidden =
                  (h.PhanLoai == "Ship" || h.PhanLoai == "Mv" || h.PhanLoai == "Tại Chỗ")
                   || (false)
                   || (false)
                   || (false)
                   || (false)
            }
        ).ToListAsync();

        return list.Select(h => new HoaDonDto
        {
            Id = h.Id,
            MaHoaDon = h.MaHoaDon,
            Ngay = h.Ngay,
            NgayGio = h.NgayGio,
            NgayShip = h.NgayShip,
            NguoiShip = h.NguoiShip,

            NgayNo = h.NgayNo,
            NgayIn = h.NgayIn,
            PhanLoai = h.PhanLoai,
            TenBan = h.TenBan,
            TenKhachHangText = h.TenKhachHangText,
            DiaChiText = h.DiaChiText,
            SoDienThoaiText = h.SoDienThoaiText,
            VoucherId = h.VoucherId,
            KhachHangId = h.KhachHangId,
            TongTien = h.TongTien,
            GiamGia = h.GiamGia,
            ThanhTien = h.ThanhTien,
            GhiChu = h.GhiChu,
            GhiChuShipper = h.GhiChuShipper,

            LastModified = h.LastModified,
            IsThanhToanHidden = h.IsThanhToanHidden
        }).ToList();
    }

    public async Task<List<HoaDonDto>> GetAllAdminAsync()
    {
        var list = await GetAllAsync();
        return list;
    }

    public async Task<Result<HoaDonDto>> CreateAsync(HoaDonDto dto)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        HoaDon? entity = null;
        HoaDonDto? after = null;

        try
        {
            var now = DateTime.Now;

            if (dto.Id == Guid.Empty)
                dto.Id = Guid.NewGuid();

            dto.PhanLoai = (dto.PhanLoai);

            if (dto.KhachHangId != null &&
                (string.IsNullOrWhiteSpace(dto.TenKhachHangText) || string.IsNullOrWhiteSpace(dto.SoDienThoaiText)))
            {
                var kh = await _context.KhachHangs.AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == dto.KhachHangId);

                if (kh != null && string.IsNullOrWhiteSpace(dto.TenKhachHangText))
                    dto.TenKhachHangText = kh.Ten;

                if (string.IsNullOrWhiteSpace(dto.SoDienThoaiText))
                {
                    dto.SoDienThoaiText = await _context.KhachHangPhones.AsNoTracking()
                        .Where(p => p.KhachHangId == dto.KhachHangId)
                        .OrderByDescending(p => p.IsDefault)
                        .Select(p => p.SoDienThoai)
                        .FirstOrDefaultAsync();
                }
            }

            if (dto.PhanLoai == "Tại chỗ" && string.IsNullOrWhiteSpace(dto.TenBan))
                return Result<HoaDonDto>.Failure("Vui lòng chọn tên bàn cho đơn Tại chỗ.");

            var khachHang = await GetOrCreateKhachHangAsync(dto, now);
            dto.KhachHangId = khachHang?.Id;

            entity = new HoaDon
            {
                Id = dto.Id,
                MaHoaDon = string.IsNullOrWhiteSpace(dto.MaHoaDon) ? MaHoaDonGenerator.Generate() : dto.MaHoaDon,
                NgayIn = dto.NgayIn,
                PhanLoai = dto.PhanLoai,
                GhiChu = dto.GhiChu,
                GhiChuShipper = dto.GhiChuShipper,
                NgayShip = dto.NgayShip,
                NguoiShip = dto.NguoiShip,

                NgayNo = dto.NgayNo,
                TenBan = dto.TenBan,
                TenKhachHangText = dto.TenKhachHangText,
                DiaChiText = dto.DiaChiText,
                SoDienThoaiText = dto.SoDienThoaiText,
                VoucherId = dto.VoucherId,
                KhachHangId = dto.KhachHangId,
                Ngay = now.Date,
                NgayGio = now,
                LastModified = now,
            };

            StringHelper.NormalizeAllStrings(entity);
            _context.HoaDons.Add(entity);

            var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
            entity.TongTien = tongTien;
            entity.GiamGia = giamGia;
            entity.ThanhTien = thanhTien;

            var ctList = dto.ChiTietHoaDons ?? new ObservableCollection<ChiTietHoaDonDto>();
            if (string.IsNullOrWhiteSpace(entity.GhiChu) && ctList.Count > 0)
            {
                var ghiChuTomTat = string.Join(", ",
                    ctList.Where(x => !string.IsNullOrWhiteSpace(x.TenSanPham))
                        .GroupBy(x => x.TenSanPham.Trim())
                        .Select(g => $"{g.Sum(x => x.SoLuong)} {g.Key}"));

                if (!string.IsNullOrWhiteSpace(ghiChuTomTat))
                    entity.GhiChu = ghiChuTomTat;
            }

            if (string.IsNullOrWhiteSpace(entity.TenBan) && dto.PhanLoai != "Tại chỗ")
            {
                var timeText = now.ToString("HH:mm");
                entity.TenBan = dto.PhanLoai switch
                {
                    "Mv" => $"Mv {timeText}",
                    "Ship" => $"Ship {timeText}",
                    "App" => $"App {timeText}",
                    _ => entity.TenBan ?? ""
                };

                entity.TenBan = StringHelper.CapitalizeEachWord(entity.TenBan ?? "");
            }

            await ApplyTonKhoByCongThucAsync(
                ExtractBienTheFromDto(dto),
                sign: -1,
                now: now,
                hoaDonId: entity.Id
            );

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            after = ToDto(entity);
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync(tx);
            await LogAdminAsync(ex);
            return Result<HoaDonDto>.Failure($"Lỗi tạo hoá đơn (đã rollback): {ex.Message}");
        }

        if (entity != null && after != null)
        {
            try
            {
                await DiscordService.SendAsync(
                    DiscordEventType.HoaDonNew,
                    $"{(entity.KhachHang?.Ten ?? entity.TenBan)} {entity.ThanhTien:N0} đ"
                );
            }
            catch
            {
                // không làm fail hóa đơn chỉ vì Discord lỗi
            }

            return Result<HoaDonDto>.Success(after, "Đã thêm hóa đơn thành công.");
        }

        return Result<HoaDonDto>.Failure("Không thể tạo hóa đơn.");
    }

    public async Task<Result<HoaDonDto>> UpdateAsync(Guid id, HoaDonDto dto)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        HoaDon? entity = null;
        HoaDonDto? after = null;

        try
        {
            entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDons)
                .Include(x => x.ChiTietHoaDonToppings)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;

            await ApplyTonKhoByCongThucAsync(
                ExtractBienTheFromEntity(entity.ChiTietHoaDons),
                sign: +1,
                now: now,
                hoaDonId: entity.Id
            );

            dto.PhanLoai = (dto.PhanLoai);

            if (dto.PhanLoai == "Tại chỗ" && string.IsNullOrWhiteSpace(dto.TenBan))
                return Result<HoaDonDto>.Failure("Vui lòng chọn tên bàn cho đơn Tại chỗ.");

            if (dto.KhachHangId != null &&
                (string.IsNullOrWhiteSpace(dto.TenKhachHangText) || string.IsNullOrWhiteSpace(dto.SoDienThoaiText)))
            {
                var kh = await _context.KhachHangs.AsNoTracking()
                    .FirstOrDefaultAsync(k => k.Id == dto.KhachHangId);

                if (kh != null && string.IsNullOrWhiteSpace(dto.TenKhachHangText))
                    dto.TenKhachHangText = kh.Ten;

                if (string.IsNullOrWhiteSpace(dto.SoDienThoaiText))
                {
                    dto.SoDienThoaiText = await _context.KhachHangPhones.AsNoTracking()
                        .Where(p => p.KhachHangId == dto.KhachHangId)
                        .OrderByDescending(p => p.IsDefault)
                        .Select(p => p.SoDienThoai)
                        .FirstOrDefaultAsync();
                }
            }

            var khachHang = await GetOrCreateKhachHangAsync(dto, now);
            dto.KhachHangId = khachHang?.Id;

            entity.PhanLoai = dto.PhanLoai;
            entity.TenBan = dto.TenBan;
            entity.TenKhachHangText = dto.TenKhachHangText;
            entity.DiaChiText = dto.DiaChiText;
            entity.SoDienThoaiText = dto.SoDienThoaiText;
            entity.GhiChu = dto.GhiChu;
            entity.GhiChuShipper = dto.GhiChuShipper;
            entity.NgayShip = dto.NgayShip;
            entity.NguoiShip = dto.NguoiShip;

            entity.NgayNo = dto.NgayNo;
            entity.NgayIn = dto.NgayIn;
            entity.VoucherId = dto.VoucherId;
            entity.KhachHangId = dto.KhachHangId;

            _context.ChiTietHoaDonToppings.RemoveRange(entity.ChiTietHoaDonToppings);
            _context.ChiTietHoaDons.RemoveRange(entity.ChiTietHoaDons);

            var (tongTien, giamGia, thanhTien) = await AddChiTietAsync(entity.Id, dto, now);
            entity.TongTien = tongTien;
            entity.GiamGia = giamGia;
            entity.ThanhTien = thanhTien;

            await ApplyTonKhoByCongThucAsync(
                ExtractBienTheFromDto(dto),
                sign: -1,
                now: now,
                hoaDonId: entity.Id
            );

            var ctList = dto.ChiTietHoaDons ?? new ObservableCollection<ChiTietHoaDonDto>();
            if (string.IsNullOrWhiteSpace(entity.GhiChu) && ctList.Count > 0)
            {
                var summary = string.Join(", ",
                    ctList.Where(x => !string.IsNullOrWhiteSpace(x.TenSanPham))
                        .GroupBy(x => x.TenSanPham.Trim())
                        .Select(g => $"{g.Sum(x => x.SoLuong)} {g.Key}"));

                if (!string.IsNullOrWhiteSpace(summary))
                    entity.GhiChu = summary;
            }

            if (dto.PhanLoai != "Tại chỗ" && string.IsNullOrWhiteSpace(entity.TenBan))
            {
                var timeText = now.ToString("HH:mm");

                entity.TenBan = dto.PhanLoai switch
                {
                    "Mv" => $"Mv {timeText}",
                    "Ship" => $"Ship {timeText}",
                    "App" => $"App {timeText}",
                    _ => entity.TenBan ?? ""
                };

                entity.TenBan = StringHelper.CapitalizeEachWord(entity.TenBan ?? "");
            }

            StringHelper.NormalizeAllStrings(entity);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            after = ToDto(entity);
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync(tx);
            await LogAdminAsync(ex);
            return Result<HoaDonDto>.Failure($"Lỗi cập nhật hoá đơn (đã rollback): {ex.Message}");
        }

        if (after != null)
            return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.");

        return Result<HoaDonDto>.Failure("Không thể cập nhật hóa đơn.");
    }

    public async Task<Result<HoaDonDto>> UpdateSingleAsync(Guid id, HoaDonDto dto)
    {
        try
        {
            var entity = await _context.HoaDons
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            entity.LastModified = dto.LastModified ?? entity.LastModified;
            entity.NgayShip = dto.NgayShip ?? entity.NgayShip;
            entity.NguoiShip = dto.NguoiShip ?? entity.NguoiShip;
            entity.NgayNo = dto.NgayNo ?? entity.NgayNo;
            entity.NgayIn = dto.NgayIn ?? entity.NgayIn;
            entity.GhiChuShipper = dto.GhiChuShipper ?? entity.GhiChuShipper;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            if (before.NgayShip == null && after.NgayShip != null && entity.NguoiShip != null)
                await DiscordService.SendAsync(DiscordEventType.DangGiaoHang, $"{entity.TenKhachHangText} {entity.DiaChiText}");
            if (before.NgayNo == null && after.NgayNo != null)
                await DiscordService.SendAsync(DiscordEventType.GhiNo, $"{entity.TenKhachHangText} đã ghi nợ");
            if (before.NgayNo != null && after.NgayNo == null)
                await DiscordService.SendAsync(DiscordEventType.GhiNo, $"Rollback {entity.TenKhachHangText}");

            return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonDto>.Failure($"Lỗi cập nhật single (đã rollback): {ex.Message}");
        }
    }

    public async Task<HoaDonDto?> GetByIdAsync(Guid id)
    {
        var h = await (
            from x in _context.HoaDons.AsNoTracking()
            join n in _context.HoaDonNos
                on x.Id equals n.Id into xn
            from n in xn.DefaultIfEmpty()
            where x.Id == id
            select new
            {
                x.Id,
                x.MaHoaDon,
                x.Ngay,
                x.NgayGio,
                x.NgayShip,
                x.NguoiShip,

                x.NgayNo,
                x.NgayIn,
                x.PhanLoai,
                x.TenBan,
                x.TenKhachHangText,
                x.DiaChiText,
                x.SoDienThoaiText,
                x.VoucherId,
                x.KhachHangId,
                x.TongTien,
                x.GiamGia,
                x.ThanhTien,
                x.GhiChu,
                x.GhiChuShipper,

                x.LastModified,

                ConLai = n != null ? n.ConLai : 0,
                DaThu = n != null ? n.DaThu : 0,

                ChiTiets = x.ChiTietHoaDons
                    .OrderBy(ct => ct.Stt)
                    .Select(ct => new ChiTietHoaDonDto
                    {
                        Id = ct.Id,
                        HoaDonId = ct.HoaDonId,
                        SanPhamBienTheId = ct.SanPhamBienTheId,
                        SanPhamId = ct.SanPhamId,
                        SoLuong = ct.SoLuong,
                        DonGia = ct.DonGia,
                        TenSanPham = ct.TenSanPham ?? "",
                        TenBienThe = ct.TenBienThe,
                        ToppingText = ct.ToppingText,
                        NoteText = ct.NoteText,
                        LastModified = ct.LastModified
                    }).ToList(),

                Toppings = x.ChiTietHoaDonToppings
                    .Select(tp => new ChiTietHoaDonToppingDto
                    {
                        Id = tp.Id,
                        HoaDonId = tp.HoaDonId,
                        ChiTietHoaDonId = tp.ChiTietHoaDonId,
                        ToppingId = tp.ToppingId,
                        Ten = tp.TenTopping,
                        SoLuong = tp.SoLuong,
                        Gia = tp.Gia,
                        LastModified = tp.LastModified
                    }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (h == null) return null;

        var dto = new HoaDonDto
        {
            Id = h.Id,
            MaHoaDon = h.MaHoaDon,
            Ngay = h.Ngay,
            NgayGio = h.NgayGio,
            NgayShip = h.NgayShip,
            NguoiShip = h.NguoiShip,

            NgayNo = h.NgayNo,
            NgayIn = h.NgayIn,
            PhanLoai = h.PhanLoai,
            TenBan = h.TenBan,

            TenKhachHangText = h.TenKhachHangText,
            DiaChiText = h.DiaChiText,
            SoDienThoaiText = h.SoDienThoaiText,
            VoucherId = h.VoucherId,
            KhachHangId = h.KhachHangId,
            TongTien = h.TongTien,
            GiamGia = h.GiamGia,
            ThanhTien = h.ThanhTien,
            GhiChu = h.GhiChu,
            GhiChuShipper = h.GhiChuShipper,

            LastModified = h.LastModified,
            ConLai = h.ConLai,
            DaThu = h.DaThu,

            ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(h.ChiTiets),
            ChiTietHoaDonToppings = h.Toppings,
        };

        foreach (var ct in dto.ChiTietHoaDons)
        {
            ct.ToppingDtos = dto.ChiTietHoaDonToppings
                .Where(tp => tp.ChiTietHoaDonId == ct.Id)
                .Select(tp => new ToppingDto
                {
                    Id = tp.ToppingId,
                    Ten = tp.Ten,
                    Gia = tp.Gia,
                    SoLuong = tp.SoLuong
                })
                .ToList();

            if (string.IsNullOrEmpty(ct.ToppingText) && ct.ToppingDtos.Any())
            {
                ct.ToppingText = string.Join(", ",
                    ct.ToppingDtos.Select(t => $"{t.Ten} x{t.SoLuong}"));
            }
        }

        if (dto.KhachHangId != null)
        {
            var khId = dto.KhachHangId.Value;
            var now = DateTime.Now;

            var duocNhanVoucher = await _context.KhachHangs
                .Where(k => k.Id == khId)
                .Select(k => k.DuocNhanVoucher)
                .FirstOrDefaultAsync();

            if (duocNhanVoucher && khId != Guid.Empty)
            {
                var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
                var firstDayPrev = firstDayCurrent.AddMonths(-1);
                var firstDayNext = firstDayCurrent.AddMonths(1);

                var agg = await _context.HoaDons
                    .AsNoTracking()
                    .Where(h =>
                        h.KhachHangId == khId &&
                        h.Ngay >= firstDayPrev &&
                        h.Ngay < firstDayNext
                    )
                    .GroupBy(h => h.Ngay >= firstDayCurrent ? 1 : 0)
                    .Select(g => new
                    {
                        IsCurrent = g.Key == 1,
                        Sum = g.Sum(h => (int?)Math.Floor(h.ThanhTien * 0.01m)) ?? 0
                    })
                    .ToListAsync();

                dto.DiemThangNay = agg
                    .Where(x => x.IsCurrent)
                    .Select(x => x.Sum)
                    .FirstOrDefault();

                dto.DiemThangTruoc = agg
                    .Where(x => !x.IsCurrent)
                    .Select(x => x.Sum)
                    .FirstOrDefault();
            }
            else
            {
                dto.DiemThangNay = -1;
                dto.DiemThangTruoc = -1;
            }

            var firstDayCurrent2 = new DateTime(now.Year, now.Month, 1);
            var firstDayNext2 = firstDayCurrent2.AddMonths(1);

            dto.DaNhanVoucher = await _context.HoaDons
                .AsNoTracking()
                .AnyAsync(hd =>
                    hd.KhachHangId == khId &&
                    hd.VoucherId != null &&
                    hd.Ngay >= firstDayCurrent2 &&
                    hd.Ngay < firstDayNext2
                );

            dto.TongNoKhachHang = await _context.HoaDonNos
                .Where(x => x.KhachHangId == khId
                         && x.Id != dto.Id
                         && x.NgayNo != null
                         && x.ConLai > 0)
                .SumAsync(x => (decimal?)x.ConLai) ?? 0;

            dto.TongDonKhacDangGiao = await _context.HoaDonNos
                .Where(x => x.KhachHangId == khId
                         && x.Id != dto.Id
                         && x.ConLai > 0
                         && x.NgayNo == null)
                .SumAsync(x => (decimal?)x.ConLai) ?? 0;
        }

        int stt = 1;
        foreach (var item in dto.ChiTietHoaDons)
        {
            item.Stt = stt++;
        }

        return dto;
    }

    public async Task<Result<HoaDonDto>> DeleteAsync(Guid id)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        HoaDon? entity = null;
        HoaDonDto? before = null;

        try
        {
            entity = await _context.HoaDons
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietHoaDons)
                .Include(x => x.ChiTietHoaDonToppings)
                .Include(x => x.ChiTietHoaDonThanhToans)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

            before = ToDto(entity);
            var now = DateTime.Now;

            await ApplyTonKhoByCongThucAsync(
                ExtractBienTheFromEntity(entity.ChiTietHoaDons),
                sign: +1,
                now: now,
                hoaDonId: entity.Id
            );

            _context.ChiTietHoaDons.RemoveRange(entity.ChiTietHoaDons);
            _context.ChiTietHoaDonToppings.RemoveRange(entity.ChiTietHoaDonToppings);
            _context.ChiTietHoaDonThanhToans.RemoveRange(entity.ChiTietHoaDonThanhToans);

            _context.HoaDons.Remove(entity);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await SafeRollbackAsync(tx);
            await LogAdminAsync(ex);
            return Result<HoaDonDto>.Failure($"Lỗi xoá hoá đơn (đã rollback): {ex.Message}");
        }

        if (entity != null && before != null)
        {
            var detailLines = entity.ChiTietHoaDons.Select(x =>
                $"- {x.TenSanPham} x{x.SoLuong}"
            );

            var toppingLines = entity.ChiTietHoaDonToppings.Select(x =>
                $"- {x.TenTopping} x{x.SoLuong}"
            );

            var message =
                $"Khách: {entity.KhachHang?.Ten ?? entity.TenBan}\n" +
                $"Thời gian: {entity.NgayGio:dd/MM/yyyy HH:mm}\n" +
                $"Tổng tiền: {entity.ThanhTien:N0} đ\n" +
                $"**Sản phẩm:**\n{string.Join("\n", detailLines)}\n" +
                (toppingLines.Any()
                    ? $"**Topping:**\n{string.Join("\n", toppingLines)}\n"
                    : "");

            try
            {
                await DiscordService.SendAsync(
                    DiscordEventType.HoaDonDel,
                    message
                );
            }
            catch
            {
            }

            return Result<HoaDonDto>.Success(before, "Xoá hóa đơn thành công.");
        }

        return Result<HoaDonDto>.Failure("Không thể xoá hóa đơn.");
    }

    public async Task<KhachHangInfoDto?> GetKhachHangInfoAsync(Guid khachHangId)
    {
        if (khachHangId == Guid.Empty)
            return null;

        var kh = await _context.KhachHangs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == khachHangId);

        if (kh == null) return null;

        var now = DateTime.Now;

        int diemThangNay = -1;
        int diemThangTruoc = -1;

        if (kh.DuocNhanVoucher)
        {
            var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
            var firstDayPrev = firstDayCurrent.AddMonths(-1);
            var firstDayNext = firstDayCurrent.AddMonths(1);

            var agg = await _context.HoaDons
                .AsNoTracking()
                .Where(h =>
                    h.KhachHangId == khachHangId &&
                    h.Ngay >= firstDayPrev &&
                    h.Ngay < firstDayNext
                )
                .GroupBy(h => h.Ngay >= firstDayCurrent ? 1 : 0)
                .Select(g => new
                {
                    IsCurrent = g.Key == 1,
                    Sum = g.Sum(h => (int?)Math.Floor(h.ThanhTien * 0.01m)) ?? 0
                })
                .ToListAsync();

            diemThangNay = agg
                .Where(x => x.IsCurrent)
                .Select(x => x.Sum)
                .FirstOrDefault();

            diemThangTruoc = agg
                .Where(x => !x.IsCurrent)
                .Select(x => x.Sum)
                .FirstOrDefault();
        }

        var firstDayCurrent2 = new DateTime(now.Year, now.Month, 1);
        var firstDayNext2 = firstDayCurrent2.AddMonths(1);

        var daNhanVoucher = await _context.HoaDons
            .AnyAsync(hd =>
                hd.KhachHangId == khachHangId
                &&
                hd.VoucherId != null &&
                hd.Ngay >= firstDayCurrent2 &&
                hd.Ngay < firstDayNext2
            );

        var tongNo = await _context.HoaDonNos
            .Where(x => x.KhachHangId == khachHangId
                     && x.NgayNo != null
                     && x.ConLai > 0)
            .SumAsync(x => (decimal?)x.ConLai) ?? 0;

        var donKhac = await _context.HoaDonNos
            .Where(x => x.KhachHangId == khachHangId
                     && x.ConLai > 0
                     && x.NgayNo == null)
            .SumAsync(x => (decimal?)x.ConLai) ?? 0;

        return new KhachHangInfoDto
        {
            KhachHangId = kh.Id,
            DuocNhanVoucher = kh.DuocNhanVoucher,
            DaNhanVoucher = daNhanVoucher,
            DiemThangNay = diemThangNay,
            DiemThangTruoc = diemThangTruoc,
            TongNo = tongNo,
            DonKhac = donKhac,
            MonYeuThich = kh.FavoriteMon
        };
    }

    public async Task<Result<HoaDonNoDto>> UpdateEscSingleAsync(Guid id, HoaDonDto dto)
    {
        try
        {
            var entity = await _context.HoaDons
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonNoDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            if (!string.IsNullOrWhiteSpace(dto.NguoiShip))
                entity.NguoiShip = dto.NguoiShip;

            entity.NgayShip = dto.NgayShip ?? entity.NgayShip;

            if (entity.NgayIn == null)
                entity.NgayIn = dto.NgayIn ?? now;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            if (before.NgayShip == null && after.NgayShip != null && entity.NguoiShip != null)
            {
                await DiscordService.SendAsync(
                    DiscordEventType.DangGiaoHang,
                    $"{entity.TenKhachHangText} {entity.DiaChiText}");
            }

            var r = _context.HoaDonNos.SingleOrDefault(x => x.Id == id);

            return Result<HoaDonNoDto>.Success(r, "Gán ship thành công.");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonNoDto>.Failure($"Lỗi gán ship: {ex.Message}");
        }
    }

    public async Task<Result<HoaDonNoDto>> UpdateRollBackSingleAsync(Guid id, HoaDonDto dto)
    {
        try
        {
            var entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDonThanhToans)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonNoDto>.Failure("Không tìm thấy hóa đơn.");

            var before = ToDto(entity);

            var payments = entity.ChiTietHoaDonThanhToans
                .ToList();

            if (payments != null && payments.Count > 0)
            {
                foreach (var p in payments)
                {
                    p.LastModified = DateTime.Now;
                }
            }

            entity.NgayNo = null;
            entity.NgayShip = null;
            entity.NguoiShip = null;
            entity.NgayIn = null;
            entity.GhiChuShipper = null;

            entity.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            if (before.NgayNo != null && after.NgayNo == null)
                await DiscordService.SendAsync(
                    DiscordEventType.GhiNo,
                    $"Rollback ghi nợ: {entity.TenKhachHangText}");

            if (before.NgayShip != null && after.NgayShip == null)
                await DiscordService.SendAsync(
                    DiscordEventType.DangGiaoHang,
                    $"Rollback ship: {entity.TenKhachHangText}");

            var r = _context.HoaDonNos.SingleOrDefault(x => x.Id == id);

            return Result<HoaDonNoDto>.Success(r, "Rollback thành công");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonNoDto>.Failure($"Lỗi rollback: {ex.Message}");
        }
    }

    public async Task<Result<HoaDonNoDto>> UpdatePrintSingleAsync(Guid id, HoaDonDto dto)
    {
        try
        {
            var entity = await _context.HoaDons
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonNoDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            if (entity.NgayIn == null)
                entity.NgayIn = dto.NgayIn ?? now;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            var r = _context.HoaDonNos.SingleOrDefault(x => x.Id == id);
            return Result<HoaDonNoDto>.Success(r, "Print thành công");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonNoDto>.Failure($"Lỗi print: {ex.Message}");
        }
    }

    public async Task<Result<HoaDonNoDto>> UpdateF12SingleAsync(Guid id, HoaDonDto dto)
    {
        try
        {
            var entity = await _context.HoaDons
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<HoaDonNoDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            if (entity.NgayNo == null)
                entity.NgayNo = dto.NgayNo ?? now;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            var r = _context.HoaDonNos.SingleOrDefault(x => x.Id == id);
            return Result<HoaDonNoDto>.Success(r, "F12 thành công");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonNoDto>.Failure($"Lỗi F12: {ex.Message}");
        }
    }

    public async Task<Result<HoaDonNoDto>> UpdateF1F4SingleAsync(Guid id, ChiTietHoaDonThanhToanDto dto)
    {
        try
        {
            if (dto.SoTien < 0)
                return Result<HoaDonNoDto>.Failure("Số tiền không được âm.");

            var hoaDon = await _context.HoaDons
                .FirstOrDefaultAsync(x => x.Id == id);

            if (hoaDon == null)
                return Result<HoaDonNoDto>.Failure("Không tìm thấy hóa đơn.");

            if (dto.LastModified != default && dto.LastModified < hoaDon.LastModified)
            {
                return Result<HoaDonNoDto>
                    .Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");
            }

            var before = ToDto(hoaDon);

            var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
                .Where(x => x.HoaDonId == id)
                .SumAsync(x => x.SoTien);

            var soTienConLai = hoaDon.ThanhTien - tongDaThanhToan;

            if (dto.SoTien > soTienConLai)
            {
                return Result<HoaDonNoDto>
                    .Failure($"Số tiền còn lại cần thu: {soTienConLai:N0}.");
            }

            DateTime now = DateTime.Now;

            DateTime ngay;
            DateTime ngayGio;

            bool quaNgay = now.Date > hoaDon.NgayGio.Date;
            bool coGhiNo = hoaDon.NgayNo != null;

            if (quaNgay && !coGhiNo)
            {
                ngay = now.Date.AddDays(-1);
                ngayGio = ngay.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else
            {
                ngay = now.Date;
                ngayGio = now;
            }

            string loaiThanhToan =
                ngay == hoaDon.Ngay ? "Thanh toán" : "Trả nợ qua ngày";

            var ct = new ChiTietHoaDonThanhToan
            {
                Id = Guid.NewGuid(),
                SoTien = dto.SoTien,

                LoaiThanhToan = loaiThanhToan,

                NgayGio = ngayGio,
                Ngay = ngay,

                HoaDonId = id,
                KhachHangId = dto.KhachHangId,
                PhuongThucThanhToanId = dto.PhuongThucThanhToanId,

                GhiChu = dto.SoTien <= 0
                    ? "Không thanh toán"
                    : dto.SoTien >= soTienConLai
                        ? "Thanh toán đủ"
                        : "Thanh toán thiếu",

                LastModified = now,
            };

            _context.ChiTietHoaDonThanhToans.Add(ct);

            hoaDon.LastModified = now;

            await _context.SaveChangesAsync();

            var after = ToDto(hoaDon);

            var r = await _context.HoaDonNos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            return Result<HoaDonNoDto>
                .Success(r, "Thanh toán thành công.");
        }
        catch (Exception ex)
        {
            await LogAdminAsync(ex);
            return Result<HoaDonNoDto>.Failure($"Lỗi thanh toán: {ex.Message}");
        }
    }
}
