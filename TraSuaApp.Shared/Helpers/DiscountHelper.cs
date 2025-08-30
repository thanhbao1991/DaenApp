namespace TraSuaApp.Shared.Helpers
{
    public static class DiscountHelper
    {
        /// <summary>
        /// Tính số tiền giảm giá.
        /// </summary>
        /// <param name="tongTien">Tổng tiền gốc</param>
        /// <param name="kieuGiam">"%" hoặc số tiền cố định</param>
        /// <param name="giaTri">Giá trị giảm</param>
        /// <param name="lamTron">Có làm tròn về bội 1000 không</param>
        public static decimal TinhGiamGia(decimal tongTien, string kieuGiam, decimal giaTri, bool lamTron = true)
        {
            decimal giamGia = 0;

            if (kieuGiam == "%")
            {
                giamGia = tongTien * (giaTri / 100m);
            }
            else
            {
                giamGia = giaTri;
            }

            // không vượt quá tổng tiền
            if (giamGia > tongTien)
                giamGia = tongTien;

            // nếu muốn làm tròn theo bội 1000
            if (lamTron && giamGia > 0)
            {
                var remainder = giamGia % 1000;
                if (remainder < 500)
                    giamGia -= remainder; // làm tròn xuống
                else
                    giamGia += (1000 - remainder); // làm tròn lên
            }

            return giamGia;
        }
    }

}
