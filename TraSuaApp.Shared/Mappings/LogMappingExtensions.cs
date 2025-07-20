using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Shared.Mappings;

public static class LogMappingExtensions
{
    public static LogDto ToDto(this Log entity)
    {
        return new LogDto
        {
            Id = entity.Id,
            ThoiGian = entity.ThoiGian,
            UserId = entity.UserId,
            UserName = entity.UserName,
            Method = entity.Method,
            Path = entity.Path,
            StatusCode = entity.StatusCode,
            Ip = entity.Ip,
            DurationMs = entity.DurationMs,
            RequestBodyShort = entity.RequestBodyShort,
            ResponseBodyShort = entity.ResponseBodyShort,
            EntityId = entity.EntityId
        };
    }

    public static Log ToEntity(this LogDto dto)
    {
        return new Log
        {
            Id = dto.Id,
            ThoiGian = dto.ThoiGian,
            UserId = dto.UserId,
            UserName = dto.UserName,
            Method = dto.Method,
            Path = dto.Path,
            StatusCode = dto.StatusCode,
            Ip = dto.Ip,
            DurationMs = dto.DurationMs,
            RequestBodyShort = dto.RequestBodyShort,
            ResponseBodyShort = dto.ResponseBodyShort,
            EntityId = dto.EntityId
        };
    }

    public static void UpdateEntity(this LogDto dto, Log entity)
    {
        entity.UserId = dto.UserId;
        entity.UserName = dto.UserName;
        entity.Method = dto.Method;
        entity.Path = dto.Path;
        entity.StatusCode = dto.StatusCode;
        entity.Ip = dto.Ip;
        entity.DurationMs = dto.DurationMs;
        entity.RequestBodyShort = dto.RequestBodyShort;
        entity.ResponseBodyShort = dto.ResponseBodyShort;
        entity.EntityId = dto.EntityId;
        // Không sửa Id và ThoiGian
    }
}