using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class SuDungNguyenLieu
{
    public Guid Id { get; set; }

    public Guid IdCongThuc { get; set; }

    public Guid IdNguyenLieu { get; set; }

    public decimal SoLuong { get; set; }

    public Guid CongThucId { get; set; }

    public Guid NguyenLieuId { get; set; }

    public virtual CongThuc CongThuc { get; set; } = null!;

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}
