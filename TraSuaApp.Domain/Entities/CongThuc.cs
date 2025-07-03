namespace TraSuaApp.Domain.Entities;

public class CongThuc
{
    public Guid Id { get; set; }
    public Guid IdSanPhamBienThe { get; set; }

    public SanPhamBienThe SanPhamBienThe { get; set; }
    public ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; }
}