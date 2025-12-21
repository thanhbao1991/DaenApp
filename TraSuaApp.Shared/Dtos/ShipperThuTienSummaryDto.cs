namespace TraSuaApp.Shared.Dtos
{
    /// <summary>
    /// Tổng kết tiền thu được của Shipper trong 1 ngày,
    /// dựa trên bảng ChiTietHoaDonThanhToans (GhiChu = "Shipper").
    /// </summary>
    public class ShipperSummaryDto
    {
        public DateTime Ngay { get; set; }

        /// <summary> Tổng tiền mặt Shipper thu trong ngày (mọi loại thanh toán, miễn là Tiền mặt). </summary>
        public decimal TienMat { get; set; }

        /// <summary> Tổng chuyển khoản Shipper thu trong ngày. </summary>
        public decimal ChuyenKhoan { get; set; }

        /// <summary> Tổng tiền Shipper thu thuộc loại "Trả nợ trong ngày". </summary>
        public decimal TraNoTrongNgay { get; set; }

        /// <summary> Tổng tiền Shipper thu thuộc loại "Trả nợ qua ngày". </summary>
        public decimal TraNoQuaNgay { get; set; }
    }
}