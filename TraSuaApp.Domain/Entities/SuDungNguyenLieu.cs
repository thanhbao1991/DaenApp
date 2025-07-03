namespace TraSuaApp.Domain.Entities;

public class SuDungNguyenLieu
{
    public Guid Id { get; set; }
    public Guid IdCongThuc { get; set; }
    public Guid IdNguyenLieu { get; set; }
    public decimal SoLuong { get; set; }

    public CongThuc CongThuc { get; set; }
    public NguyenLieu NguyenLieu { get; set; }
}