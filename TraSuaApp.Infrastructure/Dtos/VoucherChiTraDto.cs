namespace TraSuaApp.Infrastructure.Dtos
{
    // DTO đọc-only để hiển thị voucher đã áp dụng cho KH
    public class VoucherChiTraDto
    {

        public DateTime Ngay { get; set; }
        public string TenVoucher { get; set; } = "";
        public decimal GiaTriApDung { get; set; }
        public Guid? HoaDonId { get; set; }
        public Guid? VoucherId { get; set; }
        public string TenKhachHang { get; set; } = "";

        
        
        
        public int Stt { get; set; }

        
        // UI

    }
}