namespace TraSuaApp.Shared.Helpers
{
    public static class LoyaltyHelper
    {
        // So diem can de duoc 1 sao
        public const int DiemMoiSao = 3000;

        // Gia tri quy doi cua 1 sao -> voucher (VD: 10000 = 10k)
        public const int GiaTriVoucherMoiSao = 10000;

        /// <summary>
        /// Tinh so sao day tu diem
        /// </summary>
        public static int TinhSoSaoDay(int diem)
        {
            if (diem < 0) return 0;
            return diem / DiemMoiSao;
        }

        /// <summary>
        /// Tinh tong gia tri voucher tu diem
        /// </summary>
        public static int TinhGiaTriVoucher(int diem)
        {
            var sao = TinhSoSaoDay(diem);
            return sao * GiaTriVoucherMoiSao;
        }
    }
}