using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameIdKhachHangToIdOld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.RenameColumn(
                name: "IdKhachHang",
                table: "KhachHangPhones",
                newName: "IdOld");

            migrationBuilder.RenameColumn(
                name: "IdKhachHang",
                table: "KhachHangAddresses",
                newName: "IdOld");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.RenameColumn(
                name: "IdOld",
                table: "KhachHangPhones",
                newName: "IdKhachHang");

            migrationBuilder.RenameColumn(
                name: "IdOld",
                table: "KhachHangAddresses",
                newName: "IdKhachHang");


        }
    }
}
