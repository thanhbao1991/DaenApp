using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class LogService : ILogService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public LogService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<LogDto>> GetAllAsync()
    {
        var list = await _context.Logs
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return _mapper.Map<List<LogDto>>(list);
    }

    public async Task<LogDto?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Logs.FindAsync(id);
        return entity == null ? null : _mapper.Map<LogDto>(entity);
    }

    public async Task<Result<LogDto>> CreateAsync(LogDto dto)
    {
        try
        {
            //var trungTen = await _context.Logs.AnyAsync(x => x.Ten == dto.Ten);
            //if (trungTen)
            //    return Result<LogDto>.Failure("Tên Log đã tồn tại.");

            var entity = _mapper.Map<Log>(dto);
            entity.Id = Guid.NewGuid();

            _context.Logs.Add(entity);
            await _context.SaveChangesAsync();

            var resultDto = _mapper.Map<LogDto>(entity);
            return Result<LogDto>.Success("Đã thêm Log.", resultDto)
                .WithId(entity.Id)
                .WithAfter(resultDto);
        }
        catch (Exception ex)
        {
            return Result<LogDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result<LogDto>> UpdateAsync(Guid id, LogDto dto)
    {
        try
        {
            var entity = await _context.Logs.FindAsync(id);
            if (entity == null)
                return Result<LogDto>.Failure("Log không tồn tại.");

            //var trungTen = await _context.Logs
            //    .AnyAsync(x => x.Ten == dto.Ten && x.Id != id);
            //if (trungTen)
            //    return Result<LogDto>.Failure("Tên Log đã tồn tại.");

            var before = _mapper.Map<LogDto>(entity);

            _mapper.Map(dto, entity);
            await _context.SaveChangesAsync();

            var after = _mapper.Map<LogDto>(entity);

            return Result<LogDto>.Success("Đã cập nhật Log.", after)
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after);
        }
        catch (Exception ex)
        {
            return Result<LogDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }

    public async Task<Result<LogDto>> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _context.Logs.FindAsync(id);
            if (entity == null)
                return Result<LogDto>.Failure("Log không tồn tại.");

            var before = _mapper.Map<LogDto>(entity);

            _context.Logs.Remove(entity);
            await _context.SaveChangesAsync();

            return Result<LogDto>.Success("Đã xoá Log.", before)
                .WithId(id)
                .WithBefore(before);
        }
        catch (Exception ex)
        {
            return Result<LogDto>.Failure(DbExceptionHelper.Handle(ex).Message);
        }
    }
}