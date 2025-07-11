﻿using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class CongThuc
{
    public Guid Id { get; set; }

    public Guid IdSanPhamBienThe { get; set; }

    public Guid SanPhamBienTheId { get; set; }

    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}
