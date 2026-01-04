public class ChiTieuHangNgayBulkCreateDto
{
    public DateTime Ngay { get; set; }
    public DateTime? NgayGio { get; set; }
    public bool BillThang { get; set; }

    public List<ChiTieuHangNgayBulkItemDto> Items { get; set; } = new();
}

public class ChiTieuHangNgayBulkItemDto
{
    public Guid NguyenLieuId { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal? ThanhTien { get; set; }
    public string? GhiChu { get; set; }
    public bool BillThang { get; set; }
}