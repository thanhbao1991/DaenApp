using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiemKhachHangLogsToChiTietHoaDonDiems2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ChiTietHoaDonPoint",
                newName: "ChiTietHoaDonPoints");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ChiTietHoaDonPoint",
                newName: "ChiTietHoaDonPoints");
        }
    }
}
