using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class R1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonNos_KhachHangId",
                table: "ChiTietHoaDonNos",
                column: "KhachHangId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonNos_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonNos",
                column: "KhachHangId",
                principalTable: "KhachHangs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonNos_KhachHangs_KhachHangId",
                table: "ChiTietHoaDonNos");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonNos_KhachHangId",
                table: "ChiTietHoaDonNos");
        }
    }
}
