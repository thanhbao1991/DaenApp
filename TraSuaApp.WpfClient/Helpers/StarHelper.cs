namespace TraSuaApp.Shared.Helpers
{
    public static class StarHelper
    {
        public static string GetStarText(decimal diem)
        {
            if (diem < 0) return string.Empty;

            decimal stars = diem / LoyaltyHelper.DiemMoiSao;
            int fullStars = (int)Math.Floor(stars);

            string starIcons;

            // Chưa đủ 1 sao đầy
            if (fullStars < 1)
            {
                starIcons = "☆";
            }
            // Từ 1 sao đầy trở lên → luôn thu gọn
            else
            {
                starIcons = $"{fullStars}★";
            }

            return $"\t{diem / 100} ({starIcons})";
        }


        //public static string GetStarText(decimal diem)
        //{
        //    if (diem < 0) return string.Empty;

        //    decimal stars = diem / LoyaltyHelper.DiemMoiSao;
        //    int fullStars = (int)Math.Floor(stars);
        //    bool halfStar = (stars - fullStars) >= 0.5m;

        //    string starIcons;
        //    if (diem < LoyaltyHelper.DiemMoiSao)
        //    {
        //        starIcons = "☆";
        //    }
        //    else
        //    {
        //        starIcons = new string('★', fullStars);
        //        if (halfStar) starIcons += "☆";
        //    }

        //    return $"\t{diem / 100} ({starIcons})";
        //}



    }
}