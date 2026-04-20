using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
public class KhachHangController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

    public KhachHangController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "khachhang", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "khachhang", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet]
    public async Task<ActionResult<Result<List<KhachHangDto>>>> GetAll()
    {
        var list = await _context.KhachHangs
            .AsNoTracking()
            .OrderByDescending(x => x.LastModified)
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .ToListAsync();

        return Result<List<KhachHangDto>>.Success(list.Select(ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Result<KhachHangDto?>>> GetById(Guid id)
    {
        var entity = await _context.KhachHangs
            .AsNoTracking()
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangDto?>.Failure($"Không tìm thấy {_friendlyName}.");

        return Result<KhachHangDto?>.Success(ToDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<Result<KhachHangDto>>> Create(KhachHangDto dto)
    {
        var validation = ValidateAndNormalize(dto);
        if (!validation.IsSuccess)
            return validation;

        var defaultPhone = dto.Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai;
        if (defaultPhone != null && dto.Ten.Trim() == defaultPhone)
        {
            var defaultAddress = dto.Addresses
                .FirstOrDefault(a => a.IsDefault)?.DiaChi
                ?? dto.Addresses.First().DiaChi;
            dto.Ten = GenerateNameFromAddress(defaultAddress);
        }

        var dtoPhones = dto.Phones.Select(x => x.SoDienThoai).ToList();

        bool trung = await _context.KhachHangPhones
            .AnyAsync(p =>

                dtoPhones.Contains(p.SoDienThoai));

        if (trung)
            return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");

        var now = DateTime.Now;
        var tim = BuildTimKiem(dto);

        var entity = new KhachHang
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            FavoriteMon = dto.FavoriteMon,
            LastModified = now,
            ThuTu = 0,
            DuocNhanVoucher = dto.DuocNhanVoucher,
            KhachHangPhones = dto.Phones.Select(p => new KhachHangPhone
            {
                Id = Guid.NewGuid(),
                SoDienThoai = p.SoDienThoai,
                IsDefault = p.IsDefault
            }).ToList(),
            KhachHangAddresses = dto.Addresses.Select(a => new KhachHangAddress
            {
                Id = Guid.NewGuid(),
                DiaChi = a.DiaChi,
                IsDefault = a.IsDefault
            }).ToList()
        };

        StringHelper.NormalizeAllStrings(entity);

        foreach (var p in entity.KhachHangPhones)
            p.KhachHangId = entity.Id;
        foreach (var a in entity.KhachHangAddresses)
            a.KhachHangId = entity.Id;

        entity.TimKiem = tim;

        _context.KhachHangs.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangDto>.Success(after, "Thêm thành công.")
            ;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Result<KhachHangDto>>> Update(Guid id, KhachHangDto dto)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        var validation = ValidateAndNormalize(dto);
        if (!validation.IsSuccess)
            return validation;

        var phonesToCheck = new List<string>();

        foreach (var dtoP in dto.Phones)
        {
            var newNorm = NormalizePhone(dtoP.SoDienThoai);
            var old = entity.KhachHangPhones.FirstOrDefault(p => p.Id == dtoP.Id);

            if (old == null)
            {
                if (!string.IsNullOrWhiteSpace(newNorm) && !phonesToCheck.Contains(newNorm))
                    phonesToCheck.Add(newNorm);
            }
            else
            {
                var oldNorm = NormalizePhone(old.SoDienThoai);
                if (!string.IsNullOrWhiteSpace(newNorm) && oldNorm != newNorm && !phonesToCheck.Contains(newNorm))
                    phonesToCheck.Add(newNorm);
            }
        }

        if (phonesToCheck.Count > 0)
        {
            bool trung = await _context.KhachHangPhones
                .AnyAsync(p =>

                    p.KhachHangId != id &&
                    phonesToCheck.Contains(p.SoDienThoai));

            if (trung)
                return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");
        }

        var before = ToDto(entity);
        var now = DateTime.Now;
        var tim = BuildTimKiem(dto);

        entity.Ten = dto.Ten;
        entity.DuocNhanVoucher = dto.DuocNhanVoucher;
        entity.FavoriteMon = dto.FavoriteMon;
        entity.LastModified = now;

        var phonesToRemove = entity.KhachHangPhones
            .Where(p => !dto.Phones.Any(dtoP => dtoP.Id == p.Id))
            .ToList();

        var addressesToRemove = entity.KhachHangAddresses
            .Where(a => !dto.Addresses.Any(dtoA => dtoA.Id == a.Id))
            .ToList();

        if (phonesToRemove.Count > 0)
            _context.KhachHangPhones.RemoveRange(phonesToRemove);

        if (addressesToRemove.Count > 0)
            _context.KhachHangAddresses.RemoveRange(addressesToRemove);

        foreach (var phoneDto in dto.Phones)
        {
            var phone = entity.KhachHangPhones.FirstOrDefault(p => p.Id == phoneDto.Id);
            if (phone != null)
            {
                phone.SoDienThoai = phoneDto.SoDienThoai;
                phone.IsDefault = phoneDto.IsDefault;
            }
            else
            {
                entity.KhachHangPhones.Add(new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    SoDienThoai = phoneDto.SoDienThoai,
                    IsDefault = phoneDto.IsDefault,
                    KhachHangId = entity.Id
                });
            }
        }

        foreach (var addrDto in dto.Addresses)
        {
            var addr = entity.KhachHangAddresses.FirstOrDefault(a => a.Id == addrDto.Id);
            if (addr != null)
            {
                addr.DiaChi = addrDto.DiaChi;
                addr.IsDefault = addrDto.IsDefault;
            }
            else
            {
                entity.KhachHangAddresses.Add(new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    DiaChi = addrDto.DiaChi,
                    IsDefault = addrDto.IsDefault,
                    KhachHangId = entity.Id
                });
            }
        }

        StringHelper.NormalizeAllStrings(entity);
        entity.TimKiem = tim;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangDto>.Success(after, "Cập nhật thành công.")


            ;
    }

    [HttpPut("{id}/single")]
    public async Task<ActionResult<Result<KhachHangDto>>> UpdateSingle(Guid id, KhachHangDto dto)
    {
        var entity = await _context.KhachHangs
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangDto>.Failure("Không tìm thấy sản phẩm.");

        var now = DateTime.Now;
        var before = ToDto(entity);

        entity.ThuTu = dto.ThuTu;
        entity.LastModified = dto.LastModified;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangDto>.Success(after, "Cập nhật hóa đơn thành công.")


            ;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<KhachHangDto>>> Delete(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        var before = ToDto(entity);

        _context.KhachHangPhones.RemoveRange(entity.KhachHangPhones);
        _context.KhachHangAddresses.RemoveRange(entity.KhachHangAddresses);
        _context.KhachHangs.Remove(entity);

        await _context.SaveChangesAsync();

        return Result<KhachHangDto>.Success(before, "Xoá thành công.")
            ;
    }

    [HttpGet("search")]
    public async Task<ActionResult<Result<List<KhachHangDto>>>> Search([FromQuery] string q, [FromQuery] int take = 30)
    {
        q = (q ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(q))
            return Result<List<KhachHangDto>>.Success(new());

        take = Math.Clamp(take, 1, 50);
        string nx = StringHelper.MyNormalizeText(q);

        var list = await _context.KhachHangs
            .AsNoTracking()
            .Where(x =>
                        EF.Functions.Like(x.TimKiem, $"%{nx}%"))
            .OrderByDescending(x => x.ThuTu)
            .Take(take)
            .Select(x => new
            {
                Ent = x,
                Phones = x.KhachHangPhones
                    .OrderByDescending(p => p.IsDefault)
                    .ThenBy(p => p.LastModified)
                    .Select(p => p.SoDienThoai)
                    .Take(3)
                    .ToList(),
                Addrs = x.KhachHangAddresses
                    .OrderByDescending(a => a.IsDefault)
                    .ThenBy(a => a.LastModified)
                    .Select(a => a.DiaChi)
                    .Take(3)
                    .ToList()
            })
            .ToListAsync();

        var result = list.Select(r => new KhachHangDto
        {
            Id = r.Ent.Id,
            Ten = r.Ent.Ten,
            FavoriteMon = r.Ent.FavoriteMon,
            LastModified = r.Ent.LastModified,
            ThuTu = r.Ent.ThuTu,
            DuocNhanVoucher = r.Ent.DuocNhanVoucher,
            Phones = r.Phones.Select(p => new KhachHangPhoneDto { SoDienThoai = p }).ToList(),
            Addresses = r.Addrs.Select(a => new KhachHangAddressDto { DiaChi = a }).ToList()
        }).ToList();

        return Result<List<KhachHangDto>>.Success(result);
    }

    private static string BuildTimKiem(KhachHangDto dto)
        => KhachHangSearchHelper.BuildTimKiem(dto);

    private string NormalizePhone(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        input = input.Trim();
        input = Regex.Replace(input, @"[^\d+]", "");

        if (input.StartsWith("+84"))
            input = "0" + input[3..];
        else if (input.StartsWith("84"))
            input = "0" + input[2..];

        return input;
    }

    private KhachHangDto ToDto(KhachHang entity)
    {
        return new KhachHangDto
        {
            Id = entity.Id,
            Ten = entity.Ten,
            FavoriteMon = entity.FavoriteMon,
            LastModified = entity.LastModified,
            ThuTu = entity.ThuTu,
            DuocNhanVoucher = entity.DuocNhanVoucher,
            //TimKiem = entity.TimKiem,
            Phones = entity.KhachHangPhones
                .Select(p => new KhachHangPhoneDto
                {
                    Id = p.Id,
                    SoDienThoai = p.SoDienThoai,
                    IsDefault = p.IsDefault
                })
                .OrderByDescending(x => x.IsDefault)
                .ToList(),
            Addresses = entity.KhachHangAddresses
                .Select(a => new KhachHangAddressDto
                {
                    Id = a.Id,
                    DiaChi = a.DiaChi,
                    IsDefault = a.IsDefault
                })
                .OrderByDescending(x => x.IsDefault)
                .ToList()
        };
    }

    private void EnsureOneDefault<T>(List<T> list, Action<T, bool> setDefault, Func<T, bool> isDefault)
    {
        if (!list.Any()) return;

        var last = list.Last();

        if (!list.Any(isDefault))
        {
            setDefault(last, true);
        }
        else
        {
            var defaults = list.Where(isDefault).ToList();
            foreach (var item in defaults)
                setDefault(item, false);

            setDefault(defaults.Last(), true);
        }
    }

    private Result<KhachHangDto> ValidateAndNormalize(KhachHangDto dto)
    {
        dto.Phones ??= new List<KhachHangPhoneDto>();
        dto.Addresses ??= new List<KhachHangAddressDto>();

        dto.Ten = dto.Ten.Trim();
        foreach (var a in dto.Addresses)
            a.DiaChi = a.DiaChi.Trim();
        foreach (var p in dto.Phones)
            p.SoDienThoai = NormalizePhone(p.SoDienThoai);

        EnsureOneDefault(dto.Phones, (p, val) => p.IsDefault = val, p => p.IsDefault);
        EnsureOneDefault(dto.Addresses, (a, val) => a.IsDefault = val, a => a.IsDefault);

        if (dto.Phones.Any(p => string.IsNullOrWhiteSpace(p.SoDienThoai)))
            return Result<KhachHangDto>.Failure("Số điện thoại không được để trống.");

        if (dto.Phones.GroupBy(p => p.SoDienThoai).Any(g => g.Count() > 1))
            return Result<KhachHangDto>.Failure($"Không được trùng số trong cùng một {_friendlyName}.");

        if (dto.Addresses.Any(a => string.IsNullOrWhiteSpace(a.DiaChi)))
            return Result<KhachHangDto>.Failure("Địa chỉ không được để trống.");

        return Result<KhachHangDto>.Success(dto);
    }

    private string GenerateNameFromAddress(string address)
    {
        var parts = address.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return address;

        var house = parts[0];
        var initials = string.Concat(parts.Skip(1).Select(w => char.ToUpperInvariant(w[0])));
        return house + initials;
    }
}
