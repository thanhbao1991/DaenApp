using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class ChiTietTuyChinhMon
{
    public Guid Id { get; set; }

    public Guid IdTuyChinhMon { get; set; }

    public string GiaTri { get; set; } = null!;

    public Guid TuyChinhMonId { get; set; }

    public virtual TuyChinhMon TuyChinhMon { get; set; } = null!;
}
