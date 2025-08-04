using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Mappings;

public static class DtoExtensions
{
    public static TDto ToDto<TDto>(this EntityBase entity)
        where TDto : DtoBase, new()
    {
        var dto = new TDto
        {
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            LastModified = entity.LastModified,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };

        if (entity is Topping topping && dto is ToppingDto toppingDto)
        {
            toppingDto.Ten = topping.Ten;
            toppingDto.Gia = topping.Gia;
            toppingDto.NgungBan = topping.NgungBan;
            toppingDto.NhomSanPhams = topping.NhomSanPhams.Select(x => x.Id).ToList();
        }

        return dto;
    }

    public static TEntity ToEntity<TEntity>(this DtoBase dto)
        where TEntity : EntityBase, new()
    {
        var entity = new TEntity
        {
            Id = dto.Id,
            CreatedAt = dto.CreatedAt,
            LastModified = dto.LastModified,
            IsDeleted = dto.IsDeleted,
            DeletedAt = dto.DeletedAt
        };

        if (dto is ToppingDto toppingDto && entity is Topping topping)
        {
            topping.Ten = toppingDto.Ten;
            topping.Gia = toppingDto.Gia;
            topping.NgungBan = toppingDto.NgungBan;
        }

        return entity;
    }

    public static TEntity ToEntity<TEntity>(this DtoBase dto, TEntity entity)
        where TEntity : EntityBase
    {
        entity.LastModified = DateTime.Now;

        if (dto is ToppingDto toppingDto && entity is Topping topping)
        {
            topping.Ten = TextSearchHelper.NormalizeText(toppingDto.Ten);
            topping.Gia = toppingDto.Gia;
            topping.NgungBan = toppingDto.NgungBan;
        }

        return entity;
    }

    public static Topping ToEntity(this ToppingDto dto, Topping entity, List<NhomSanPham>? allNhom)
    {
        dto.ToEntity(entity); // dùng lại logic chung

        if (allNhom != null)
        {
            entity.NhomSanPhams = allNhom
                .Where(x => dto.NhomSanPhams.Contains(x.Id))
                .ToList();
        }

        return entity;
    }
}
