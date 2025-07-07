using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

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
            .Include(kh => kh.Phones)
            .Include(kh => kh.Addresss)
            .OrderBy(kh => kh.Ten)
            .ToListAsync();

        return _mapper.Map<List<KhachHangDto>>(list);
    }

    public async Task<KhachHangDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(kh => kh.Phones)
            .Include(kh => kh.Addresss)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : _mapper.Map<KhachHangDto>(entity);
    }

    public async Task<KhachHangDto> CreateAsync(KhachHangDto dto)
    {
        try
        {
            var entity = _mapper.Map<KhachHang>(dto);
            entity.Id = Guid.NewGuid();

            entity.Phones = dto.Phones?
                .Select(p => new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = entity.Id,
                    SoDienThoai = p.SoDienThoai,
                    IsDefault = p.IsDefault
                }).ToList() ?? new();

            if (entity.Phones.Count(p => p.IsDefault) > 1)
                throw new Exception("Chỉ được chọn một số điện thoại mặc định.");

            entity.Addresss = dto.Addresses?
                .Select(d => new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = entity.Id,
                    DiaChi = d.DiaChi,
                    IsDefault = d.IsDefault
                }).ToList() ?? new();

            if (entity.Addresss.Count(d => d.IsDefault) > 1)
                throw new Exception("Chỉ được chọn một địa chỉ mặc định.");

            _context.KhachHangs.Add(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<KhachHangDto>(entity);
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
                .Include(kh => kh.Phones)
                .Include(kh => kh.Addresss)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return new Result { IsSuccess = false, Message = "Không tìm thấy khách hàng." };

            _mapper.Map(dto, entity);

            _context.KhachHangPhones.RemoveRange(entity.Phones);
            entity.Phones = dto.Phones?
                .Select(p => new KhachHangPhone
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = id,
                    SoDienThoai = p.SoDienThoai,
                    IsDefault = p.IsDefault
                }).ToList() ?? new();

            if (entity.Phones.Count(p => p.IsDefault) > 1)
                return new Result { IsSuccess = false, Message = "Chỉ được chọn một số điện thoại mặc định." };

            _context.KhachHangAddresses.RemoveRange(entity.Addresss);
            entity.Addresss = dto.Addresses?
                .Select(d => new KhachHangAddress
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = id,
                    DiaChi = d.DiaChi,
                    IsDefault = d.IsDefault
                }).ToList() ?? new();

            if (entity.Addresss.Count(d => d.IsDefault) > 1)
                return new Result { IsSuccess = false, Message = "Chỉ được chọn một địa chỉ mặc định." };

            await _context.SaveChangesAsync();
            return new Result { IsSuccess = true, Message = "Cập nhật thành công." };
        }
        catch (Exception ex)
        {
            throw DbExceptionHelper.Handle(ex);
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var entity = await _context.KhachHangs
            .Include(kh => kh.Phones)
            .Include(kh => kh.Addresss)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return new Result { IsSuccess = false, Message = "Không tìm thấy khách hàng." };

        _context.KhachHangPhones.RemoveRange(entity.Phones);
        _context.KhachHangAddresses.RemoveRange(entity.Addresss);
        _context.KhachHangs.Remove(entity);

        await _context.SaveChangesAsync();
        return new Result { IsSuccess = true, Message = "Xoá thành công." };
    }
}