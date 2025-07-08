using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services
{
    public class KhachHangService : IKhachHangService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public KhachHangService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<KhachHangDto>> GetAllAsync()
        {
            var list = await _context.KhachHangs
                .Include(kh => kh.KhachHangPhones)
                .Include(kh => kh.KhachHangAddresses)
                .OrderBy(kh => kh.Ten)
                .ToListAsync();

            return _mapper.Map<List<KhachHangDto>>(list);
        }

        public async Task<KhachHangDto?> GetByIdAsync(Guid id)
        {
            var entity = await _context.KhachHangs
                .Include(kh => kh.KhachHangPhones)
                .Include(kh => kh.KhachHangAddresses)
                .FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : _mapper.Map<KhachHangDto>(entity);
        }

        public async Task<Result> CreateAsync(KhachHangDto dto)
        {
            try
            {
                var entity = _mapper.Map<KhachHang>(dto);
                entity.Id = Guid.NewGuid();

                entity.KhachHangPhones = dto.Phones?
                    .Select(p => new KhachHangPhone
                    {
                        Id = Guid.NewGuid(),
                        IdKhachHang = entity.Id,
                        SoDienThoai = p.SoDienThoai,
                        IsDefault = p.IsDefault
                    }).ToList() ?? new();

                if (entity.KhachHangPhones.Count(p => p.IsDefault) > 1)
                    return Result.Failure("Chỉ được chọn một số điện thoại mặc định.");

                entity.KhachHangAddresses = dto.Addresses?
                    .Select(d => new KhachHangAddress
                    {
                        Id = Guid.NewGuid(),
                        IdKhachHang = entity.Id,
                        DiaChi = d.DiaChi,
                        IsDefault = d.IsDefault
                    }).ToList() ?? new();

                if (entity.KhachHangAddresses.Count(d => d.IsDefault) > 1)
                    return Result.Failure("Chỉ được chọn một địa chỉ mặc định.");

                _context.KhachHangs.Add(entity);
                await _context.SaveChangesAsync();

                dto.Id = entity.Id; // Gán lại Id mới để controller dùng
                return Result.Success("Đã thêm khách hàng.");
            }
            catch (Exception ex)
            {
                throw DbExceptionHelper.Handle(ex);
            }
        }

        public async Task<Result> UpdateAsync(Guid id, KhachHangDto dto)
        {
            try
            {
                var entity = await _context.KhachHangs
                    .Include(kh => kh.KhachHangPhones)
                    .Include(kh => kh.KhachHangAddresses)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return Result.Failure("Không tìm thấy khách hàng.");

                _mapper.Map(dto, entity);

                _context.KhachHangPhones.RemoveRange(entity.KhachHangPhones);
                entity.KhachHangPhones = dto.Phones?
                    .Select(p => new KhachHangPhone
                    {
                        Id = Guid.NewGuid(),
                        IdKhachHang = id,
                        SoDienThoai = p.SoDienThoai,
                        IsDefault = p.IsDefault
                    }).ToList() ?? new();

                if (entity.KhachHangPhones.Count(p => p.IsDefault) > 1)
                    return Result.Failure("Chỉ được chọn một số điện thoại mặc định.");

                _context.KhachHangAddresses.RemoveRange(entity.KhachHangAddresses);
                entity.KhachHangAddresses = dto.Addresses?
                    .Select(d => new KhachHangAddress
                    {
                        Id = Guid.NewGuid(),
                        IdKhachHang = id,
                        DiaChi = d.DiaChi,
                        IsDefault = d.IsDefault
                    }).ToList() ?? new();

                if (entity.KhachHangAddresses.Count(d => d.IsDefault) > 1)
                    return Result.Failure("Chỉ được chọn một địa chỉ mặc định.");

                await _context.SaveChangesAsync();
                return Result.Success("Cập nhật thành công.");
            }
            catch (Exception ex)
            {
                throw DbExceptionHelper.Handle(ex);
            }
        }

        public async Task<Result> DeleteAsync(Guid id)
        {
            var entity = await _context.KhachHangs
                .Include(kh => kh.KhachHangPhones)
                .Include(kh => kh.KhachHangAddresses)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result.Failure("Không tìm thấy khách hàng.");

            _context.KhachHangPhones.RemoveRange(entity.KhachHangPhones);
            _context.KhachHangAddresses.RemoveRange(entity.KhachHangAddresses);
            _context.KhachHangs.Remove(entity);

            await _context.SaveChangesAsync();
            return Result.Success("Xoá thành công.");
        }
    }
}