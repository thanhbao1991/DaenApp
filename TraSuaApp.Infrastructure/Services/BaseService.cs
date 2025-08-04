using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Domain.Interfaces;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Mappings;

namespace TraSuaApp.Infrastructure.Services;

public abstract class BaseService<TEntity, TDto>
    where TEntity : EntityBase, new()
    where TDto : DtoBase, new()
{
    protected readonly IRepository<TEntity> _repo;
    protected readonly IAppDbContext _context;
    protected string _friendlyName = "Dữ liệu";

    protected BaseService(IAppDbContext context)
    {
        _context = context;
        _repo = context.GetRepository<TEntity>();
    }

    public virtual async Task<List<TDto>> GetAllAsync()
    {
        var list = await _repo.All()
            .OrderBy(x => x.LastModified)
            .ToListAsync();

        return list.Select(x => x.ToDto<TDto>()).ToList();
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repo.FindAsync(id);
        return entity?.ToDto<TDto>();
    }

    public virtual async Task<Result<TDto>> CreateAsync(TDto dto)
    {
        var validate = await ValidateAsync(dto);
        if (!validate.IsSuccess)
            return Result<TDto>.Failure(validate.Message);

        var entity = dto.ToEntity<TEntity>();
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        entity.LastModified = DateTime.Now;

        await BeforeCreateAsync(entity, dto);

        _repo.Add(entity);
        await _context.SaveChangesAsync();

        return Result<TDto>.Success(entity.ToDto<TDto>()).WithId(entity.Id);
    }

    public virtual async Task<Result<TDto>> UpdateAsync(Guid id, TDto dto)
    {
        var entity = await _repo.FindAsync(id);
        if (entity == null)
            return Result<TDto>.Failure($"{_friendlyName} không tồn tại.");

        var validate = await ValidateAsync(dto, id);
        if (!validate.IsSuccess)
            return Result<TDto>.Failure(validate.Message);

        var before = entity.ToDto<TDto>();

        dto.ToEntity(entity);
        entity.LastModified = DateTime.Now;

        await BeforeUpdateAsync(entity, dto);
        _repo.Update(entity);
        await _context.SaveChangesAsync();

        return Result<TDto>.Success(entity.ToDto<TDto>())
                           .WithBefore(before)
                           .WithAfter(entity.ToDto<TDto>())
                           .WithId(entity.Id);
    }

    public virtual async Task<Result<TDto>> DeleteAsync(Guid id)
    {
        var entity = await _repo.FindAsync(id);
        if (entity == null)
            return Result<TDto>.Failure($"{_friendlyName} không tồn tại.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.Now;

        _repo.Update(entity);
        await _context.SaveChangesAsync();

        return Result<TDto>.Success(entity.ToDto<TDto>()).WithId(entity.Id);
    }

    public virtual async Task<Result<TDto>> RestoreAsync(Guid id)
    {
        var entity = await _repo.FindAsync(id);
        if (entity == null)
            return Result<TDto>.Failure($"{_friendlyName} không tồn tại.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;

        _repo.Update(entity);
        await _context.SaveChangesAsync();

        return Result<TDto>.Success(entity.ToDto<TDto>()).WithId(entity.Id);
    }

    public virtual async Task<List<TDto>> GetUpdatedSince(DateTime lastSync)
    {
        var list = await _repo.All()
            .Where(x => x.LastModified > lastSync)
            .ToListAsync();

        return list.Select(x => x.ToDto<TDto>()).ToList();
    }

    protected virtual Task<Result<TDto>> ValidateAsync(TDto dto, Guid? id = null)
        => Task.FromResult(Result<TDto>.Success(dto));

    protected virtual Task BeforeCreateAsync(TEntity entity, TDto dto)
        => Task.CompletedTask;

    protected virtual Task BeforeUpdateAsync(TEntity entity, TDto dto)
        => Task.CompletedTask;
}
