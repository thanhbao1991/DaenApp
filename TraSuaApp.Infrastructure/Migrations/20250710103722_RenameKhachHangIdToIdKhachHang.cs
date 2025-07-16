using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameKhachHangIdToIdKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KhachHangId",
                table: "KhachHangPhones",
                newName: "IdKhachHang");

            migrationBuilder.RenameColumn(
                name: "KhachHangId",
                table: "KhachHangAddresses",
                newName: "IdKhachHang");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdKhachHang",
                table: "KhachHangPhones",
                newName: "KhachHangId");

            migrationBuilder.RenameColumn(
                name: "IdKhachHang",
                table: "KhachHangAddresses",
                newName: "KhachHangId");
        }
    }
}
