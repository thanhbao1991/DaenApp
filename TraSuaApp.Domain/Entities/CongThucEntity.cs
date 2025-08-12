using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class CongThuc
{
    public Guid Id { get; set; }

    public Guid SanPhamBienTheId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}
