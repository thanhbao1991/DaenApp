namespace TraSuaApp.Domain.Entities
{
    public partial class SuDungNguyenLieu
    {
        public Guid Id { get; set; }

        public decimal SoLuong { get; set; }

        // FK -> NguyenLieuBanHang (đơn vị bán)
        public Guid NguyenLieuId { get; set; }

        public Guid CongThucId { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? LastModified { get; set; }

        public virtual CongThuc CongThuc { get; set; } = null!;

        // Trước đây: public virtual NguyenLieu NguyenLieu { get; set; } = null!;
        // Giờ trỏ sang NguyenLieuBanHang
        public virtual NguyenLieuBanHang NguyenLieu { get; set; } = null!;
    }
}