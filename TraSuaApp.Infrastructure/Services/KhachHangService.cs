#pragma warning disable CS8618
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;


public class KhachHangService : IKhachHangService
{
    private readonly AppDbContext _context;
    private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];
    public KhachHangService(AppDbContext context)
    {
        _context = context;
    }

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
            IsDeleted = entity.IsDeleted,
            LastModified = entity.LastModified,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,

            DuocNhanVoucher = entity.DuocNhanVoucher,
            Phones = entity.KhachHangPhones.Select(p => new KhachHangPhoneDto
            {
                Id = p.Id,
                SoDienThoai = p.SoDienThoai,
                IsDefault = p.IsDefault
            }).OrderByDescending(x => x.IsDefault).ToList(),
            Addresses = entity.KhachHangAddresses.Select(a => new KhachHangAddressDto
            {
                Id = a.Id,
                DiaChi = a.DiaChi,
                IsDefault = a.IsDefault
            }).OrderByDescending(x => x.IsDefault).ToList()
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
        dto.Ten = (dto.Ten.Trim());
        foreach (var a in dto.Addresses)
            a.DiaChi = (a.DiaChi.Trim());
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

    public async Task<List<KhachHangDto>> GetAllAsync()
    {
        var list = await _context.KhachHangs.AsNoTracking()
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<KhachHangDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        return entity == null ? null : ToDto(entity);
    }
    private string GenerateNameFromAddress(string address)
    {
        var parts = address
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return address;

        var house = parts[0];         // "02"
        var initials = string.Concat(
            parts.Skip(1)
                 .Select(w => char.ToUpperInvariant(w[0]))
        );                            // "LTK"
        return house + initials;      // "02LTK"
    }
    public async Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto)
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

        bool trung = await _context.KhachHangPhones
            .AnyAsync(p => dto.Phones.Select(x => x.SoDienThoai).Contains(p.SoDienThoai));

        if (trung)
            return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");

        var entity = new KhachHang
        {
            Id = Guid.NewGuid(),
            Ten = dto.Ten,
            OldId = dto.OldId,
            IsDeleted = false,
            LastModified = DateTime.Now,
            CreatedAt = DateTime.Now,
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

        foreach (var p in entity.KhachHangPhones)
            p.KhachHangId = entity.Id;
        foreach (var a in entity.KhachHangAddresses)
            a.KhachHangId = entity.Id;

        _context.KhachHangs.Add(entity);
        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangDto>.Success(after, "Thêm thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return Result<KhachHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        if (dto.LastModified < entity.LastModified)
            return Result<KhachHangDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

        var validation = ValidateAndNormalize(dto);
        if (!validation.IsSuccess)
            return validation;

        bool trung = await _context.KhachHangPhones
            .AnyAsync(p => dto.Phones.Select(x => x.SoDienThoai).Contains(p.SoDienThoai) && p.KhachHangId != id);

        if (trung)
            return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");

        var before = ToDto(entity);

        entity.Ten = dto.Ten;
        entity.LastModified = DateTime.Now;
        entity.DuocNhanVoucher = dto.DuocNhanVoucher;

        // Xoá cũ
        var phonesToRemove = entity.KhachHangPhones
            .Where(p => !dto.Phones.Any(dtoP => dtoP.Id == p.Id)).ToList();
        var addressesToRemove = entity.KhachHangAddresses
            .Where(a => !dto.Addresses.Any(dtoA => dtoA.Id == a.Id)).ToList();
        phonesToRemove.ForEach(p => entity.KhachHangPhones.Remove(p));
        addressesToRemove.ForEach(a => entity.KhachHangAddresses.Remove(a));

        // Cập nhật hoặc thêm mới
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

        await _context.SaveChangesAsync();

        var after = await GetByIdAsync(id);
        return Result<KhachHangDto>.Success(after!, "Cập nhật thành công.")
            .WithId(id)
            .WithBefore(before)
            .WithAfter(after);
    }

    public async Task<Result<KhachHangDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null || entity.IsDeleted)
            return Result<KhachHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        var before = ToDto(entity);

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;
        await _context.SaveChangesAsync();

        return Result<KhachHangDto>.Success(before, "Xoá thành công.")
            .WithId(before.Id)
            .WithBefore(before);
    }

    public async Task<Result<KhachHangDto>> RestoreAsync(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<KhachHangDto>.Failure($"Không tìm thấy {_friendlyName}.");

        if (!entity.IsDeleted)
            return Result<KhachHangDto>.Failure($"{_friendlyName} này chưa bị xoá.");

        entity.IsDeleted = false;
        entity.LastModified = DateTime.Now;
        entity.DeletedAt = null;

        await _context.SaveChangesAsync();

        var after = ToDto(entity);
        return Result<KhachHangDto>.Success(after, "Khôi phục thành công.")
            .WithId(after.Id)
            .WithAfter(after);
    }

    public async Task<List<KhachHangDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _context.KhachHangs.AsNoTracking()
            .Include(x => x.KhachHangPhones)
            .Include(x => x.KhachHangAddresses)
            .Where(x => x.LastModified > lastSync)
            .OrderByDescending(x => x.LastModified)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }
}
