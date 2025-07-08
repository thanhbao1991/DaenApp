using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

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

    public async Task<Result<NhomSanPhamDto>> CreateAsync(NhomSanPhamDto dto)
    {
        try
        {
            var trungTen = await _context.NhomSanPhams.AnyAsync(x => x.Ten == dto.Ten);
            if (trungTen)
                return Result<NhomSanPhamDto>.Failure("Tên nhóm sản phẩm đã tồn tại.");

            var entity = _mapper.Map<NhomSanPham>(dto);
            entity.Id = Guid.NewGuid();

            _context.NhomSanPhams.Add(entity);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<NhomSanPhamDto>(entity);
            return Result<NhomSanPhamDto>.Success("Đã thêm nhóm sản phẩm.", resultDto)
                .WithId(entity.Id)
                .WithAfter(resultDto);
        }
        catch (Exception ex)
        {
            return Result<NhomSanPhamDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result<NhomSanPhamDto>> UpdateAsync(Guid id, NhomSanPhamDto dto)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return Result<NhomSanPhamDto>.Failure("Nhóm sản phẩm không tồn tại.");

            var trungTen = await _context.NhomSanPhams
                .AnyAsync(x => x.Ten == dto.Ten && x.Id != id);
            if (trungTen)
                return Result<NhomSanPhamDto>.Failure("Tên nhóm sản phẩm đã tồn tại.");

            var before = _mapper.Map<NhomSanPhamDto>(entity);

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            var after = _mapper.Map<NhomSanPhamDto>(entity);

            return Result<NhomSanPhamDto>.Success("Đã cập nhật nhóm sản phẩm.", after)
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after);
        }
        catch (Exception ex)
        {
            return Result<NhomSanPhamDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result<NhomSanPhamDto>> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.NhomSanPhams.FindAsync(id);
            if (entity == null)
                return Result<NhomSanPhamDto>.Failure("Nhóm sản phẩm không tồn tại.");

            var before = _mapper.Map<NhomSanPhamDto>(entity);

            _context.NhomSanPhams.Remove(entity);
            await _context.SaveChangesAsync();

            return Result<NhomSanPhamDto>.Success("Đã xoá nhóm sản phẩm.", before)
                .WithId(id)
                .WithBefore(before);
        }
        catch (Exception ex)
        {
            return Result<NhomSanPhamDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }
}