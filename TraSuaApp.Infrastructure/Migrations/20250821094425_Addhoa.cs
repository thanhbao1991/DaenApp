using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Addhoa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonPoints_HoaDonId",
                table: "ChiTietHoaDonPoints",
                column: "HoaDonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonPoints_HoaDons_HoaDonId",
                table: "ChiTietHoaDonPoints",
                column: "HoaDonId",
                principalTable: "HoaDons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonPoints_HoaDons_HoaDonId",
                table: "ChiTietHoaDonPoints");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonPoints_HoaDonId",
                table: "ChiTietHoaDonPoints");
        }
    }
}
