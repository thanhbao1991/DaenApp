namespace TraSuaApp.Shared.Dtos
{
    // DTO đọc-only để hiển thị voucher đã áp dụng cho KH
    public class VoucherChiTraDto
    {
        public Guid Id { get; set; }
        public DateTime Ngay { get; set; }           // map từ CreatedAt
        public string TenVoucher { get; set; } = "";
        public decimal GiaTriApDung { get; set; }
        public Guid? HoaDonId { get; set; }
        public Guid? VoucherId { get; set; }
        public string TenKhachHang { get; set; } = "";


        // UI
        public int Stt { get; set; }
    }
}