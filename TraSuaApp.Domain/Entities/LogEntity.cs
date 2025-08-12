using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class Log
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Method { get; set; }

    public string? Path { get; set; }

    public int StatusCode { get; set; }

    public string? Ip { get; set; }

    public long? DurationMs { get; set; }

    public string? RequestBodyShort { get; set; }

    public string? ResponseBodyShort { get; set; }

    public Guid? EntityId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }
}
