using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class TuyChinhMon
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool ChoPhepChonNhieuGiaTri { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietTuyChinhMon> ChiTietTuyChinhMons { get; set; } = new List<ChiTietTuyChinhMon>();
}
