namespace TraSuaApp.Domain.Entities;

public partial class CongThuc : EntityBase
{

    public Guid SanPhamIdBienThe { get; set; }

    public Guid SanPhamBienTheId { get; set; }

    public virtual SanPhamBienThe? SanPhamBienThe { get; set; }

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}





