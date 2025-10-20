namespace TraSuaApp.Shared.Helpers
{
    public static class StarHelper
    {
        /// <summary>
        /// Tra ve chuoi hien thi so sao dua tren diem.
        /// - Neu diem = -1 → tra ve chuoi rong
        /// - Moi DiemMoiSao diem = 1 sao "★"
        /// - Neu du >= DiemMoiSao/2 thi them "☆" (nua sao)
        /// - Neu < DiemMoiSao diem thi chi hien thi "☆"
        /// </summary>
        public static string GetStarText(decimal diem)
        {
            if (diem < 0) return string.Empty;

            decimal stars = diem / LoyaltyHelper.DiemMoiSao;
            int fullStars = (int)Math.Floor(stars);
            bool halfStar = (stars - fullStars) >= 0.5m;

            string starIcons;
            if (diem < LoyaltyHelper.DiemMoiSao)
            {
                starIcons = "☆";
            }
            else
            {
                starIcons = new string('★', fullStars);
                if (halfStar) starIcons += "☆";
            }

            return $"\t{diem / 100} ({starIcons})";
        }
    }
}