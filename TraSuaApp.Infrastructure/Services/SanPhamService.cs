using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class SanPhamService : ISanPhamService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SanPhamService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<SanPhamDto>> GetAllAsync()
    {
        var list = await _context.SanPhams
            .Include(sp => sp.SanPhamBienThes)
            .Include(sp => sp.IdNhomSanPham)
            .ToListAsync();

        return _mapper.Map<List<SanPhamDto>>(list);
    }

    public async Task<SanPhamDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.SanPhamBienThes)
            .Include(sp => sp.IdNhomSanPhamNavigation)
            .FirstOrDefaultAsync(x => x.Id == id);

        return entity == null ? null : _mapper.Map<SanPhamDto>(entity);
    }

    public async Task<Result<SanPhamDto>> CreateAsync(SanPhamDto dto)
    {
        try
        {
            var entity = _mapper.Map<SanPham>(dto);
            entity.Id = Guid.NewGuid();

            foreach (var bt in entity.SanPhamBienThes)
            {
                bt.Id = Guid.NewGuid();
                bt.IdSanPham = entity.Id;
            }

            _context.SanPhams.Add(entity);
            await _context.SaveChangesAsync();

            var resultDto = await GetByIdAsync(entity.Id);
            return Result<SanPhamDto>.Success("Đã thêm sản phẩm.", resultDto!)
                .WithId(entity.Id)
                .WithAfter(resultDto);
        }
        catch (DbUpdateException ex)
        {
            return Result<SanPhamDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
        catch (Exception ex)
        {
            return Result<SanPhamDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<SanPhamDto>> UpdateAsync(Guid id, SanPhamDto dto)
    {
        try
        {
            var entity = await _context.SanPhams
                .Include(sp => sp.SanPhamBienThes)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return Result<SanPhamDto>.Failure("Không tìm thấy sản phẩm.");

            var before = _mapper.Map<SanPhamDto>(entity);
            var bienTheMoi = dto.BienThe;
            var bienTheCu = entity.SanPhamBienThes.ToList();

            if (bienTheMoi.Count(bt => bt.MacDinh) > 1)
                return Result<SanPhamDto>.Failure("Chỉ được chọn một biến thể mặc định.");

            _mapper.Map(dto, entity);

            foreach (var bt in entity.SanPhamBienThes)
                bt.MacDinh = false;

            await _context.SaveChangesAsync();

            foreach (var btCu in bienTheCu)
            {
                if (!bienTheMoi.Any(bt => bt.Id == btCu.Id))
                {
                    bool daSuDung = await _context.ChiTietHoaDons
                        .AnyAsync(ct => ct.SanPhamBienTheId == btCu.Id);

                    if (!daSuDung)
                        _context.SanPhamBienThes.Remove(btCu);
                }
            }

            foreach (var btMoi in bienTheMoi)
            {
                var btEntity = entity.SanPhamBienThes.FirstOrDefault(x => x.Id == btMoi.Id);
                if (btEntity != null)
                {
                    btEntity.TenBienThe = btMoi.TenBienThe;
                    btEntity.GiaBan = btMoi.GiaBan;
                    btEntity.MacDinh = btMoi.MacDinh;
                }
                else
                {
                    var newBt = _mapper.Map<SanPhamBienThe>(btMoi);
                    newBt.Id = Guid.NewGuid();
                    newBt.IdSanPham = entity.Id;
                    _context.SanPhamBienThes.Add(newBt);
                }
            }

            await _context.SaveChangesAsync();

            var after = await GetByIdAsync(id);
            return Result<SanPhamDto>.Success("Đã cập nhật sản phẩm.", after!)
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after);
        }
        catch (DbUpdateException ex)
        {
            return Result<SanPhamDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
        catch (Exception ex)
        {
            return Result<SanPhamDto>.Failure(ex.Message);
        }
    }

    public async Task<Result<SanPhamDto>> DeleteAsync(Guid id)
    {
        var entity = await _context.SanPhams
            .Include(sp => sp.SanPhamBienThes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<SanPhamDto>.Failure("Không tìm thấy sản phẩm.");

        var before = _mapper.Map<SanPhamDto>(entity);

        _context.SanPhamBienThes.RemoveRange(entity.SanPhamBienThes);
        _context.SanPhams.Remove(entity);
        await _context.SaveChangesAsync();

        return Result<SanPhamDto>.Success("Đã xoá sản phẩm.", before)
            .WithId(id)
            .WithBefore(before);
    }
}