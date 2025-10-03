using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToLogllll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropIndex(
                name: "IX_KhachHangGiaBans_KhachHangId",
                table: "KhachHangGiaBans");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangGiaBans_IsDeleted_LastModified",
                table: "KhachHangGiaBans",
                columns: new[] { "IsDeleted", "LastModified" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangGiaBans_KH_SanPhamBienThe",
                table: "KhachHangGiaBans",
                columns: new[] { "KhachHangId", "SanPhamBienTheId" },
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonNhaps_HoaDonNhaps_HoaDonNhapId",
                table: "ChiTietHoaDonNhaps");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonNhaps_NguyenLieus_NguyenLieuId",
                table: "ChiTietHoaDonNhaps");

            migrationBuilder.DropForeignKey(
                name: "FK_NoHoaDons_HoaDons_HoaDonId",
                table: "ChiTietHoaDonNos");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPointLogs_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonPoints");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDons_HoaDons_HoaDonId",
                table: "ChiTietHoaDons");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDons_SanPhamBienThes_SanPhamBienTheId",
                table: "ChiTietHoaDons");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_HoaDons_HoaDonId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonToppings_HoaDons_HoaDonId",
                table: "ChiTietHoaDonToppings");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonToppings_Toppings_ToppingId",
                table: "ChiTietHoaDonToppings");

            migrationBuilder.DropForeignKey(
                name: "FK_VoucherLogs_HoaDons_HoaDonId",
                table: "ChiTietHoaDonVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_VoucherLogs_Vouchers_VoucherId",
                table: "ChiTietHoaDonVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietTuyChinhMons_TuyChinhMons_TuyChinhMonId",
                table: "ChiTietTuyChinhMons");

            migrationBuilder.DropForeignKey(
                name: "FK_CongThucs_SanPhamBienThes_SanPhamBienTheId",
                table: "CongThucs");

            migrationBuilder.DropForeignKey(
                name: "FK_HoaDons_KhachHangs_KhachHangId",
                table: "HoaDons");

            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangAddresses_KhachHangs_IdKhachHang",
                table: "KhachHangAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangGiaBans_KhachHangs_KhachHangId",
                table: "KhachHangGiaBans");

            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangGiaBans_SanPhamBienThes_SanPhamBienTheId",
                table: "KhachHangGiaBans");

            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangPhones_KhachHangs_IdKhachHang",
                table: "KhachHangPhones");

            migrationBuilder.DropForeignKey(
                name: "FK_LichSuNhapXuatKhos_NguyenLieus_NguyenLieuId",
                table: "LichSuNhapXuatKhos");

            migrationBuilder.DropForeignKey(
                name: "FK_SanPhamBienThes_SanPhams_SanPhamId",
                table: "SanPhamBienThes");

            migrationBuilder.DropForeignKey(
                name: "FK_SanPhams_NhomSanPhams_NhomSanPhamId",
                table: "SanPhams");

            migrationBuilder.DropForeignKey(
                name: "FK_SuDungNguyenLieus_CongThucs_CongThucId",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId",
                table: "SuDungNguyenLieus");

            migrationBuilder.DropForeignKey(
                name: "FK_ToppingNhomSanPhamLinks_NhomSanPham",
                table: "ToppingNhomSanPhamLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ToppingNhomSanPhamLinks_Topping",
                table: "ToppingNhomSanPhamLinks");

            migrationBuilder.DropIndex(
                name: "IX_KhachHangGiaBans_IsDeleted_LastModified",
                table: "KhachHangGiaBans");

            migrationBuilder.DropIndex(
                name: "IX_KhachHangGiaBans_KH_SanPhamBienThe",
                table: "KhachHangGiaBans");

            migrationBuilder.CreateIndex(
                name: "IX_KhachHangGiaBans_KhachHangId",
                table: "KhachHangGiaBans",
                column: "KhachHangId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonNhaps_HoaDonNhaps_HoaDonNhapId",
                table: "ChiTietHoaDonNhaps",
                column: "HoaDonNhapId",
                principalTable: "HoaDonNhaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonNhaps_NguyenLieus_NguyenLieuId",
                table: "ChiTietHoaDonNhaps",
                column: "NguyenLieuId",
                principalTable: "NguyenLieus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NoHoaDons_HoaDons_HoaDonId",
                table: "ChiTietHoaDonNos",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPointLogs_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonPoints",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDons_HoaDons_HoaDonId",
                table: "ChiTietHoaDons",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDons_SanPhamBienThes_SanPhamBienTheId",
                table: "ChiTietHoaDons",
                column: "SanPhamBienTheId",
                principalTable: "SanPhamBienThes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_HoaDons_HoaDonId",
                table: "ChiTietHoaDonThanhToans",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PaymentMethods_PaymentMethodId",
                table: "ChiTietHoaDonThanhToans",
                column: "PhuongThucThanhToanId",
                principalTable: "PhuongThucThanhToans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonToppings_HoaDons_HoaDonId",
                table: "ChiTietHoaDonToppings",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonToppings_Toppings_ToppingId",
                table: "ChiTietHoaDonToppings",
                column: "ToppingId",
                principalTable: "Toppings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherLogs_HoaDons_HoaDonId",
                table: "ChiTietHoaDonVouchers",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VoucherLogs_Vouchers_VoucherId",
                table: "ChiTietHoaDonVouchers",
                column: "VoucherId",
                principalTable: "Vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietTuyChinhMons_TuyChinhMons_TuyChinhMonId",
                table: "ChiTietTuyChinhMons",
                column: "TuyChinhMonId",
                principalTable: "TuyChinhMons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CongThucs_SanPhamBienThes_SanPhamBienTheId",
                table: "CongThucs",
                column: "SanPhamBienTheId",
                principalTable: "SanPhamBienThes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HoaDons_KhachHangs_KhachHangId",
                table: "HoaDons",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangAddresses_KhachHangs_IdKhachHang",
                table: "KhachHangAddresses",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangGiaBans_KhachHangs_KhachHangId",
                table: "KhachHangGiaBans",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangGiaBans_SanPhamBienThes_SanPhamBienTheId",
                table: "KhachHangGiaBans",
                column: "SanPhamBienTheId",
                principalTable: "SanPhamBienThes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangPhones_KhachHangs_IdKhachHang",
                table: "KhachHangPhones",
                column: "KhachHangID",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuNhapXuatKhos_NguyenLieus_NguyenLieuId",
                table: "LichSuNhapXuatKhos",
                column: "NguyenLieuId",
                principalTable: "NguyenLieus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SanPhamBienThes_SanPhams_SanPhamId",
                table: "SanPhamBienThes",
                column: "SanPhamId",
                principalTable: "SanPhams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SanPhams_NhomSanPhams_NhomSanPhamId",
                table: "SanPhams",
                column: "NhomSanPhamID",
                principalTable: "NhomSanPhams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SuDungNguyenLieus_CongThucs_CongThucId",
                table: "SuDungNguyenLieus",
                column: "CongThucId",
                principalTable: "CongThucs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SuDungNguyenLieus_NguyenLieus_NguyenLieuId",
                table: "SuDungNguyenLieus",
                column: "NguyenLieuId",
                principalTable: "NguyenLieus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ToppingNhomSanPhamLinks_NhomSanPham",
                table: "ToppingNhomSanPhamLinks",
                column: "NhomSanPhamID",
                principalTable: "NhomSanPhams",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ToppingNhomSanPhamLinks_Topping",
                table: "ToppingNhomSanPhamLinks",
                column: "ToppingID",
                principalTable: "Toppings",
                principalColumn: "Id");
        }
    }
}
