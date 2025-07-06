using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TraSuaApp.Infrastructure.Helpers
{
    public static class DbExceptionHelper
    {
        public static Exception Handle(Exception ex)
        {
            if (ex is DbUpdateException dbEx && dbEx.InnerException is SqlException sqlEx)
            {
                if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                {
                    var messages = new Dictionary<string, string>
                    {
                        ["IX_CustomerPhoneNumber_Default"] = "Chỉ một dòng được đánh dấu là mặc định.",
                        ["IX_ShippingAddress_Default"] = "Chỉ một dòng được đánh dấu là mặc định.",
                        ["IX_SanPham_Ten"] = "Tên sản phẩm đã tồn tại.",
                        ["IX_BienThe_Ten_IdSanPham"] = "Tên size đã tồn tại.",
                        ["IX_BienThe_MacDinh"] = "Chỉ một dòng được đánh dấu là mặc định.",
                        ["IX_TaiKhoan_TenDangNhap"] = "Tên đăng nhập đã tồn tại.",
                        ["IX_NhomSanPham_Ten"] = "Tên nhóm đã tồn tại.",
                        ["IX_Topping_Ten"] = "Tên topping đã tồn tại."


                    };

                    foreach (var pair in messages)
                    {
                        if (sqlEx.Message.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                            return new Exception(pair.Value);
                    }
                }

                // Lỗi SQL khác
                return new Exception($"Lỗi CSDL: {sqlEx.Message}");
            }

            // Lỗi không phải từ SQL
            return new Exception(ex.Message);
        }
    }
}