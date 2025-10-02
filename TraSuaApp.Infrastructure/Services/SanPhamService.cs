using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services
{
    public class SanPhamService : ISanPhamService
    {
        private readonly AppDbContext _context;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["SanPham"];

        public SanPhamService(AppDbContext context)
        {
            _context = context;
        }

        // Chuyển entity => DTO
        private SanPhamDto ToDto(SanPham entity)
        {
            return new SanPhamDto
            {
                Id = entity.Id,
                Ten = entity.Ten,
                DinhLuong = entity.DinhLuong,
                VietTat = entity.VietTat,
                TenKhongVietTat = entity.TenKhongVietTat,
                DaBan = entity.DaBan,
                NgungBan = entity.NgungBan,
                TichDiem = entity.TichDiem,
                OldId = entity.OldId,
                NhomSanPhamId = entity.NhomSanPhamId,
                TenNhomSanPham = entity.NhomSanPham?.Ten,
                CreatedAt = entity.CreatedAt,
                LastModified = entity.LastModified,
                DeletedAt = entity.DeletedAt,
                IsDeleted = entity.IsDeleted,
                BienThe = entity.SanPhamBienThes
                                    .Select(x => new SanPhamBienTheDto
                                    {
                                        Id = x.Id,
                                        SanPhamId = x.SanPhamId,
                                        TenBienThe = x.TenBienThe,
                                        GiaBan = x.GiaBan,
                                        MacDinh = x.MacDinh
                                    })
                                    .ToList()
            };
        }


        public async Task<Result<SanPhamDto>> UpdateSingleAsync(Guid id, SanPhamDto dto)
        {
            var entity = await _context.SanPhams
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<SanPhamDto>.Failure("Không tìm thấy hóa đơn.");

            if (dto.LastModified < entity.LastModified)
                return Result<SanPhamDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            entity.DaBan = dto.DaBan;
            entity.LastModified = dto.LastModified;

            await _context.SaveChangesAsync();


            var after = ToDto(entity);
            return Result<SanPhamDto>.Success(after, "Cập nhật hóa đơn thành công.")
                            .WithId(id)
                            .WithBefore(before)
                            .WithAfter(after);
        }


        public async Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto)
        {
            var entity = new SanPham
            {
                Id = Guid.NewGuid(),
                Ten = dto.Ten.Trim(),
                DinhLuong = dto.DinhLuong?.Trim(),
                VietTat = dto.VietTat?.Trim(),
                TenKhongVietTat = dto.TenKhongVietTat,

                DaBan = dto.DaBan,
                NgungBan = dto.NgungBan,
                TichDiem = dto.TichDiem,
                OldId = dto.OldId,
                NhomSanPhamId = dto.NhomSanPhamId,
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                IsDeleted = false,
                SanPhamBienThes = dto.BienThe.Select(b => new SanPhamBienThe
                {
                    Id = Guid.NewGuid(),
                    TenBienThe = b.TenBienThe,
                    GiaBan = b.GiaBan,
                    MacDinh = b.MacDinh,
                    SanPhamId = Guid.Empty // sẽ gán bên dưới
                }).ToList()
            };

            // Gán khóa ngoại cho biến thể
            foreach (var bt in entity.SanPhamBienThes)
                bt.SanPhamId = entity.Id;

            _context.SanPhams.Add(entity);
            await _context.SaveChangesAsync();

            var after = ToDto(entity);
            return Result<SanPhamDto>.Success(after, $"Đã thêm {_friendlyName.ToLower()} thành công.")
                                     .WithId(after.Id)
                                     .WithAfter(after);
        }

        public async Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto)
        {
            var entity = await _context.SanPhams
                .Include(x => x.SanPhamBienThes)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

            if (dto.LastModified < entity.LastModified)
                return Result<SanPhamDto>.Failure("Dữ liệu đã được cập nhật ở nơi khác. Vui lòng tải lại.");

            var before = ToDto(entity);

            // Cập nhật các trường
            entity.Ten = dto.Ten.Trim();
            entity.DinhLuong = dto.DinhLuong?.Trim();
            entity.VietTat = dto.VietTat?.Trim();
            entity.TenKhongVietTat = dto.TenKhongVietTat;

            entity.DaBan = dto.DaBan;
            entity.NgungBan = dto.NgungBan;
            entity.TichDiem = dto.TichDiem;
            entity.NhomSanPhamId = dto.NhomSanPhamId;
            entity.LastModified = DateTime.Now;

            // Cập nhật danh sách biến thể: xóa hết rồi thêm lại
            entity.SanPhamBienThes.Clear();
            foreach (var b in dto.BienThe)
            {
                entity.SanPhamBienThes.Add(new SanPhamBienThe
                {
                    Id = Guid.NewGuid(),
                    TenBienThe = b.TenBienThe,
                    GiaBan = b.GiaBan,
                    MacDinh = b.MacDinh,
                    SanPhamId = entity.Id
                });
            }

            await _context.SaveChangesAsync();

            var after = ToDto(entity);
            return Result<SanPhamDto>.Success(after, $"Cập nhật {_friendlyName.ToLower()} thành công.")
                                     .WithId(id)
                                     .WithBefore(before)
                                     .WithAfter(after);
        }

        public async Task<Result<SanPhamDto>> DeleteAsync(Guid id)
        {
            var entity = await _context.SanPhams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null || entity.IsDeleted)
                return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

            var before = ToDto(entity);

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.Now;
            entity.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();

            return Result<SanPhamDto>.Success(before, $"Xoá {_friendlyName.ToLower()} thành công.")
                                     .WithId(before.Id)
                                     .WithBefore(before);
        }

        public async Task<Result<SanPhamDto>> RestoreAsync(Guid id)
        {
            var entity = await _context.SanPhams
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<SanPhamDto>.Failure($"Không tìm thấy {_friendlyName.ToLower()}.");

            if (!entity.IsDeleted)
                return Result<SanPhamDto>.Failure($"{_friendlyName} này chưa bị xoá.");

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.LastModified = DateTime.Now;

            await _context.SaveChangesAsync();

            var after = ToDto(entity);
            return Result<SanPhamDto>.Success(after, $"Khôi phục {_friendlyName.ToLower()} thành công.")
                                     .WithId(after.Id)
                                     .WithAfter(after);
        }



        public async Task<List<SanPhamDto>> GetAllAsync()
        {
            return await _context.SanPhams.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.LastModified)
                .Select(entity => new SanPhamDto
                {
                    Id = entity.Id,
                    Ten = entity.Ten,
                    DinhLuong = entity.DinhLuong,
                    VietTat = entity.VietTat,
                    TenKhongVietTat = entity.TenKhongVietTat,

                    DaBan = entity.DaBan,
                    NgungBan = entity.NgungBan,
                    TichDiem = entity.TichDiem,
                    OldId = entity.OldId,
                    NhomSanPhamId = entity.NhomSanPhamId,
                    TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                    CreatedAt = entity.CreatedAt,
                    LastModified = entity.LastModified,
                    DeletedAt = entity.DeletedAt,
                    IsDeleted = entity.IsDeleted,
                    BienThe = entity.SanPhamBienThes.Select(b => new SanPhamBienTheDto
                    {
                        Id = b.Id,
                        SanPhamId = b.SanPhamId,
                        TenBienThe = b.TenBienThe,
                        GiaBan = b.GiaBan,
                        MacDinh = b.MacDinh
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<SanPhamDto?> GetByIdAsync(Guid id)
        {
            return await _context.SanPhams.AsNoTracking()
                .Where(x => x.Id == id && !x.IsDeleted)
                .Select(entity => new SanPhamDto
                {
                    Id = entity.Id,
                    Ten = entity.Ten,
                    DinhLuong = entity.DinhLuong,
                    VietTat = entity.VietTat,
                    TenKhongVietTat = entity.TenKhongVietTat,

                    DaBan = entity.DaBan,
                    NgungBan = entity.NgungBan,
                    TichDiem = entity.TichDiem,
                    OldId = entity.OldId,
                    NhomSanPhamId = entity.NhomSanPhamId,
                    TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                    CreatedAt = entity.CreatedAt,
                    LastModified = entity.LastModified,
                    DeletedAt = entity.DeletedAt,
                    IsDeleted = entity.IsDeleted,
                    BienThe = entity.SanPhamBienThes.Select(b => new SanPhamBienTheDto
                    {
                        Id = b.Id,
                        SanPhamId = b.SanPhamId,
                        TenBienThe = b.TenBienThe,
                        GiaBan = b.GiaBan,
                        MacDinh = b.MacDinh
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<SanPhamDto>> GetUpdatedSince(DateTime lastSync)
        {
            return await _context.SanPhams.AsNoTracking()
                .Where(x => x.LastModified > lastSync)
                .OrderByDescending(x => x.LastModified)
                .Select(entity => new SanPhamDto
                {
                    Id = entity.Id,
                    Ten = entity.Ten,
                    DinhLuong = entity.DinhLuong,
                    VietTat = entity.VietTat,
                    TenKhongVietTat = entity.TenKhongVietTat,

                    DaBan = entity.DaBan,
                    NgungBan = entity.NgungBan,
                    TichDiem = entity.TichDiem,
                    OldId = entity.OldId,
                    NhomSanPhamId = entity.NhomSanPhamId,
                    TenNhomSanPham = entity.NhomSanPham != null ? entity.NhomSanPham.Ten : null,
                    CreatedAt = entity.CreatedAt,
                    LastModified = entity.LastModified,
                    DeletedAt = entity.DeletedAt,
                    IsDeleted = entity.IsDeleted,
                    BienThe = entity.SanPhamBienThes.Select(b => new SanPhamBienTheDto
                    {
                        Id = b.Id,
                        SanPhamId = b.SanPhamId,
                        TenBienThe = b.TenBienThe,
                        GiaBan = b.GiaBan,
                        MacDinh = b.MacDinh
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}
