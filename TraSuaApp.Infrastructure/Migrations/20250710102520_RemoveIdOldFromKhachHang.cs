using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIdOldFromKhachHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerPhoneNumber_Default",
                table: "KhachHangPhones");

            migrationBuilder.DropIndex(
                name: "IX_ShippingAddress_Default",
                table: "KhachHangAddresses");

            migrationBuilder.DropColumn(
                name: "IdOld",
                table: "KhachHangPhones");

            migrationBuilder.DropColumn(
                name: "IdOld",
                table: "KhachHangAddresses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
