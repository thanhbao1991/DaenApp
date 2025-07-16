using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    public partial class RenameKhachHangForeignKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🟟 KhachHangPhones
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerPhoneNumbers_KhachHangs_KhachHangId",
                table: "KhachHangPhones");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangPhones_KhachHangs_IdKhachHang",
                table: "KhachHangPhones",
                column: "IdKhachHang",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 🟟 KhachHangAddresses
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingAddresses_KhachHangs_KhachHangId",
                table: "KhachHangAddresses");

            migrationBuilder.AddForeignKey(
                name: "FK_KhachHangAddresses_KhachHangs_IdKhachHang",
                table: "KhachHangAddresses",
                column: "IdKhachHang",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 🟟 KhachHangPhones (quay ngược)
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangPhones_KhachHangs_IdKhachHang",
                table: "KhachHangPhones");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerPhoneNumbers_KhachHangs_KhachHangId",
                table: "KhachHangPhones",
                column: "IdKhachHang",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // 🟟 KhachHangAddresses (quay ngược)
            migrationBuilder.DropForeignKey(
                name: "FK_KhachHangAddresses_KhachHangs_IdKhachHang",
                table: "KhachHangAddresses");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingAddresses_KhachHangs_KhachHangId",
                table: "KhachHangAddresses",
                column: "IdKhachHang",
                principalTable: "KhachHangs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}