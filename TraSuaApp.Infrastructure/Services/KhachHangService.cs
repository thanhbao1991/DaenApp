#pragma warning disable CS8618
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services
{
    public class KhachHangService : IKhachHangService
    {
        private readonly AppDbContext _context;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

        public KhachHangService(AppDbContext context)
        {
            _context = context;
        }

        // ================== SEARCH ==================
        public async Task<List<KhachHangDto>> SearchAsync(string q, int take = 30)
        {
            q = (q ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(q)) return new();

            take = Math.Clamp(take, 1, 50);
            string nx = StringHelper.MyNormalizeText(q); // giống WPF

            var list = await _context.KhachHangs
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                            EF.Functions.Like(x.TimKiem, $"%{nx}%"))  // giống Contains
                .OrderByDescending(x => x.ThuTu)                     // chỉ ThuTu
                .Take(take)
                .Select(x => new
                {
                    Ent = x,
                    Phones = x.KhachHangPhones
                                .OrderByDescending(p => p.IsDefault)
                                .ThenBy(p => p.CreatedAt)
                                .Select(p => p.SoDienThoai)
                                .Take(3)
                                .ToList(),
                    Addrs = x.KhachHangAddresses
                                .OrderByDescending(a => a.IsDefault)
                                .ThenBy(a => a.CreatedAt)
                                .Select(a => a.DiaChi)
                                .Take(3)
                                .ToList()
                })
                .ToListAsync();

            return list.Select(r => new KhachHangDto
            {
                Id = r.Ent.Id,
                Ten = r.Ent.Ten,
                FavoriteMon = r.Ent.FavoriteMon,
                CreatedAt = r.Ent.CreatedAt,
                LastModified = r.Ent.LastModified,
                ThuTu = r.Ent.ThuTu,
                DuocNhanVoucher = r.Ent.DuocNhanVoucher,
                Phones = r.Phones.Select(p => new KhachHangPhoneDto { SoDienThoai = p }).ToList(),
                Addresses = r.Addrs.Select(a => new KhachHangAddressDto { DiaChi = a }).ToList()
            }).ToList();
        }

        // ================== UPDATE SINGLE ==================
        public async Task<Result<KhachHangDto>> UpdateSingleAsync(Guid id, KhachHangDto dto)
        {
            var entity = await _context.KhachHangs
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<KhachHangDto>.Failure("Không tìm thấy sản phẩm.");

            if (dto.LastModified < entity.LastModified)
                return Result<KhachHangDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            entity.ThuTu = dto.ThuTu;
            entity.LastModified = dto.LastModified;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);
            return Result<KhachHangDto>.Success(after, "Cập nhật hóa đơn thành công.")
                            .WithId(id)
                            .WithBefore(before)
                            .WithAfter(after);
        }

        // ================== HELPERS ==================
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
                IsDeleted = entity.IsDeleted,
                LastModified = entity.LastModified,
                CreatedAt = entity.CreatedAt,
                DeletedAt = entity.DeletedAt,
                ThuTu = entity.ThuTu,
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

        // ================== CREATE ==================
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

            var dtoPhones = dto.Phones.Select(x => x.SoDienThoai).ToList();

            // ❗ Chỉ chặn trùng với khách còn hoạt động (IsDeleted = false)
            bool trung = await _context.KhachHangPhones
                .AnyAsync(p =>
                    !p.KhachHang.IsDeleted &&
                    dtoPhones.Contains(p.SoDienThoai));

            if (trung)
                return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");

            var now = DateTime.Now;
            // ➜ TÍNH TimKiem TRƯỚC
            var tim = BuildTimKiem(dto);

            var entity = new KhachHang
            {
                Id = Guid.NewGuid(),
                Ten = dto.Ten,
                FavoriteMon = dto.FavoriteMon,
                OldId = dto.OldId,
                IsDeleted = false,
                LastModified = now,
                CreatedAt = now,
                ThuTu = 0,
                DuocNhanVoucher = dto.DuocNhanVoucher,
                // TimKiem sẽ gán SAU khi normalize để không bị strip
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

            // Dọn chuỗi trước
            StringHelper.NormalizeAllStrings(entity);

            // Gán FK
            foreach (var p in entity.KhachHangPhones)
                p.KhachHangId = entity.Id;
            foreach (var a in entity.KhachHangAddresses)
                a.KhachHangId = entity.Id;

            // ➜ GÁN LẠI TimKiem SAU CÙNG (giữ dấu ';')
            entity.TimKiem = tim;

            _context.KhachHangs.Add(entity);
            await _context.SaveChangesAsync();

            var after = ToDto(entity);
            return Result<KhachHangDto>.Success(after, "Thêm thành công.")
                .WithId(after.Id)
                .WithAfter(after);
        }

        // ================== UPDATE ==================
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

            // ===== CHỈ CHECK TRÙNG CHO SĐT MỚI / SĐT BỊ SỬA =====
            var phonesToCheck = new List<string>();

            foreach (var dtoP in dto.Phones)
            {
                var newNorm = NormalizePhone(dtoP.SoDienThoai);
                var old = entity.KhachHangPhones.FirstOrDefault(p => p.Id == dtoP.Id);

                if (old == null)
                {
                    // SĐT mới thêm
                    if (!string.IsNullOrWhiteSpace(newNorm) && !phonesToCheck.Contains(newNorm))
                        phonesToCheck.Add(newNorm);
                }
                else
                {
                    // SĐT cũ nhưng có thể đã bị sửa
                    var oldNorm = NormalizePhone(old.SoDienThoai);
                    if (!string.IsNullOrWhiteSpace(newNorm) && oldNorm != newNorm && !phonesToCheck.Contains(newNorm))
                        phonesToCheck.Add(newNorm);
                }
            }

            if (phonesToCheck.Count > 0)
            {
                bool trung = await _context.KhachHangPhones
                    .AnyAsync(p =>
                        !p.KhachHang.IsDeleted &&          // chỉ khách còn hoạt động
                        p.KhachHangId != id &&             // bỏ qua chính khách này
                        phonesToCheck.Contains(p.SoDienThoai));

                if (trung)
                    return Result<KhachHangDto>.Failure($"Số điện thoại đã tồn tại ở {_friendlyName} khác.");
            }
            // ===== HẾT PHẦN CHECK TRÙNG =====

            var before = ToDto(entity);
            var now = DateTime.Now;

            // ➜ TÍNH TimKiem TRƯỚC
            var tim = BuildTimKiem(dto);

            // Cập nhật các trường đơn
            entity.Ten = dto.Ten;
            entity.FavoriteMon = dto.FavoriteMon;
            entity.LastModified = now;
            entity.DuocNhanVoucher = dto.DuocNhanVoucher;

            // ===== XOÁ CÁC PHONE / ADDRESS KHÔNG CÒN TRONG DTO =====
            var phonesToRemove = entity.KhachHangPhones
                .Where(p => !dto.Phones.Any(dtoP => dtoP.Id == p.Id))
                .ToList();

            var addressesToRemove = entity.KhachHangAddresses
                .Where(a => !dto.Addresses.Any(dtoA => dtoA.Id == a.Id))
                .ToList();

            if (phonesToRemove.Count > 0)
            {
                _context.KhachHangPhones.RemoveRange(phonesToRemove);
            }

            if (addressesToRemove.Count > 0)
            {
                _context.KhachHangAddresses.RemoveRange(addressesToRemove);
            }
            // ===== HẾT PHẦN XOÁ =====

            // Cập nhật/Thêm mới phone
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

            // Cập nhật/Thêm mới address
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

            // Dọn chuỗi trước
            StringHelper.NormalizeAllStrings(entity);

            // ➜ GÁN LẠI TimKiem SAU CÙNG (giữ dấu ';')
            entity.TimKiem = tim;

            await _context.SaveChangesAsync();

            var after = await GetByIdAsync(id);
            return Result<KhachHangDto>.Success(after!, "Cập nhật thành công.")
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after);
        }
        // ================== DELETE ==================
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

        // ================== RESTORE ==================
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

        // ================== GET ALL ==================
        public async Task<List<KhachHangDto>> GetAllAsync()
        {
            var baseItems = await _context.KhachHangs
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.LastModified)
                .Select(x => new KhachHangDto
                {
                    Id = x.Id,
                    Ten = x.Ten,
                    FavoriteMon = x.FavoriteMon,
                    IsDeleted = x.IsDeleted,
                    LastModified = x.LastModified,
                    CreatedAt = x.CreatedAt,
                    DeletedAt = x.DeletedAt,
                    ThuTu = x.ThuTu,
                    DuocNhanVoucher = x.DuocNhanVoucher,
                    Phones = new List<KhachHangPhoneDto>(),
                    Addresses = new List<KhachHangAddressDto>()
                })
                .ToListAsync();

            if (baseItems.Count == 0) return baseItems;

            var ids = baseItems.Select(x => x.Id).ToList();

            static IEnumerable<List<T>> Chunk<T>(IReadOnlyList<T> src, int size)
            {
                for (int i = 0; i < src.Count; i += size)
                    yield return src.Skip(i).Take(Math.Min(size, src.Count - i)).ToList();
            }

            var phoneDict = new Dictionary<Guid, List<KhachHangPhoneDto>>(ids.Count);
            foreach (var part in Chunk(ids, 2000))
            {
                var phones = await _context.KhachHangPhones
                    .AsNoTracking()
                    .Where(p => part.Contains(p.KhachHangId))
                    .Select(p => new { p.KhachHangId, p.Id, p.SoDienThoai, p.IsDefault })
                    .ToListAsync();

                foreach (var g in phones.GroupBy(x => x.KhachHangId))
                {
                    phoneDict[g.Key] = g
                        .OrderByDescending(x => x.IsDefault)
                        .Select(x => new KhachHangPhoneDto
                        {
                            Id = x.Id,
                            SoDienThoai = x.SoDienThoai,
                            IsDefault = x.IsDefault
                        })
                        .ToList();
                }
            }

            var addrDict = new Dictionary<Guid, List<KhachHangAddressDto>>(ids.Count);
            foreach (var part in Chunk(ids, 2000))
            {
                var addrs = await _context.KhachHangAddresses
                    .AsNoTracking()
                    .Where(a => part.Contains(a.KhachHangId))
                    .Select(a => new { a.KhachHangId, a.Id, a.DiaChi, a.IsDefault })
                    .ToListAsync();

                foreach (var g in addrs.GroupBy(x => x.KhachHangId))
                {
                    addrDict[g.Key] = g
                        .OrderByDescending(x => x.IsDefault)
                        .Select(x => new KhachHangAddressDto
                        {
                            Id = x.Id,
                            DiaChi = x.DiaChi,
                            IsDefault = x.IsDefault
                        })
                        .ToList();
                }
            }

            foreach (var kh in baseItems)
            {
                if (phoneDict.TryGetValue(kh.Id, out var ph)) kh.Phones = ph;
                if (addrDict.TryGetValue(kh.Id, out var ad)) kh.Addresses = ad;
            }

            return baseItems;
        }

        // ================== GET BY ID ==================
        public async Task<KhachHangDto?> GetByIdAsync(Guid id)
        {
            return await _context.KhachHangs.AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(entity => new KhachHangDto
                {
                    Id = entity.Id,
                    Ten = entity.Ten,
                    FavoriteMon = entity.FavoriteMon,
                    IsDeleted = entity.IsDeleted,
                    LastModified = entity.LastModified,
                    CreatedAt = entity.CreatedAt,
                    DeletedAt = entity.DeletedAt,
                    ThuTu = entity.ThuTu,
                    DuocNhanVoucher = entity.DuocNhanVoucher,
                    Phones = entity.KhachHangPhones
                        .Select(p => new KhachHangPhoneDto
                        {
                            Id = p.Id,
                            SoDienThoai = p.SoDienThoai,
                            IsDefault = p.IsDefault
                        })
                        .OrderByDescending(p => p.IsDefault)
                        .ToList(),
                    Addresses = entity.KhachHangAddresses
                        .Select(a => new KhachHangAddressDto
                        {
                            Id = a.Id,
                            DiaChi = a.DiaChi,
                            IsDefault = a.IsDefault
                        })
                        .OrderByDescending(a => a.IsDefault)
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        // ================== SYNC ==================
        public async Task<List<KhachHangDto>> GetUpdatedSince(DateTime lastSync)
        {
            return await _context.KhachHangs.AsNoTracking()
                .Where(x => x.LastModified > lastSync)
                .OrderByDescending(x => x.LastModified)
                .Select(entity => new KhachHangDto
                {
                    Id = entity.Id,
                    Ten = entity.Ten,
                    FavoriteMon = entity.FavoriteMon,
                    IsDeleted = entity.IsDeleted,
                    LastModified = entity.LastModified,
                    CreatedAt = entity.CreatedAt,
                    DeletedAt = entity.DeletedAt,
                    DuocNhanVoucher = entity.DuocNhanVoucher,
                    Phones = entity.KhachHangPhones
                        .Select(p => new KhachHangPhoneDto
                        {
                            Id = p.Id,
                            SoDienThoai = p.SoDienThoai,
                            IsDefault = p.IsDefault
                        })
                        .OrderByDescending(p => p.IsDefault)
                        .ToList(),
                    Addresses = entity.KhachHangAddresses
                        .Select(a => new KhachHangAddressDto
                        {
                            Id = a.Id,
                            DiaChi = a.DiaChi,
                            IsDefault = a.IsDefault
                        })
                        .OrderByDescending(a => a.IsDefault)
                        .ToList()
                })
                .ToListAsync();
        }
    }
}