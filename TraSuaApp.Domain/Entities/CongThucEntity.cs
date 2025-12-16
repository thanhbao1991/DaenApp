namespace TraSuaApp.Domain.Entities;

public partial class CongThuc
{
    public Guid Id { get; set; }

    // Món/biến thể nào
    public Guid SanPhamBienTheId { get; set; }

    // Thông tin nhận diện công thức
    public string? Ten { get; set; }         // VD: "Công thức chuẩn", "Ít đường"
    public string? Loai { get; set; }        // VD: "Default", "ItDuong", ...
    public bool IsDefault { get; set; }      // Công thức đang dùng để trừ kho

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastModified { get; set; }

    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; }
        = new List<SuDungNguyenLieu>();
}