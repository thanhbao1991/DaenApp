using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class NhomSanPhamService : INhomSanPhamService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public NhomSanPhamService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<NhomSanPhamDto>> GetAllAsync()
    {
        var list = await _context.NhomSanPhams
            .OrderBy(x => x.Ten)
            .ToListAsync();

        return _mapper.Map<List<NhomSanPhamDto>>(list);
    }

    public async Task<NhomSanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.NhomSanPhams.FindAsync(id);
        return entity == null ? null : _mapper.Map<NhomSanPhamDto>(entity);
    }

    public async Task<Result> CreateAsync(NhomSanPhamDto dto)
    {
        try
        {
            var trungTen = await _context.NhomSanPhams.AnyAsync(x => x.Ten == dto.Ten);
            if (trungTen)
                return Result.Failure("Tên nhóm sản phẩm đã tồn tại.");

            var entity = _mapper.Map<NhomSanPham>(dto);
            entity.Id = Guid.NewGuid();

            _context.NhomSanPhams.Add(entity);
            await _context.SaveChangesAsync();

            return Result.Success("Đã thêm nhóm sản phẩm.");
        }
        catch (Exception ex)
        {
            return Result.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return Result.Failure("Nhóm sản phẩm không tồn tại.");

            var trungTen = await _context.NhomSanPhams
                .AnyAsync(x => x.Ten == dto.Ten && x.Id != id);

            if (trungTen)
                return Result.Failure("Tên nhóm sản phẩm đã tồn tại.");

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();
            return Result.Success("Đã cập nhật nhóm sản phẩm.");
        }
        catch (Exception ex)
        {
            return Result.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return Result.Failure("Nhóm sản phẩm không tồn tại.");

            _context.NhomSanPhams.Remove(entity);
            await _context.SaveChangesAsync();
            return Result.Success("Đã xoá nhóm sản phẩm.");
        }
        catch (Exception ex)
        {
            return Result.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }
}