using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class Log : EntityBase
{

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

    [NotMapped]
    public Guid? EntityId { get; set; }
}




