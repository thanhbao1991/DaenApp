namespace TraSuaApp.Domain.Entities;

public partial class SuDungNguyenLieu : EntityBase
{

    public Guid CongThucId { get; set; }

    public Guid NguyenLieuId { get; set; }

    public decimal SoLuong { get; set; }


    public virtual CongThuc? CongThuc { get; set; }

    public virtual NguyenLieu? NguyenLieu { get; set; }
}




