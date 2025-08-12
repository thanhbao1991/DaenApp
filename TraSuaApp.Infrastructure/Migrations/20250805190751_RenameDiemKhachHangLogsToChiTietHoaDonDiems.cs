using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameChiTietHoaDonPointsToChiTietHoaDonDiems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DiemKhachHangLogs",
                newName: "ChiTietHoaDonPoints");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "DiemKhachHangLogs",
                newName: "ChiTietHoaDonPoints");
        }
    }
}
