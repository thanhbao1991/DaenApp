using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Seed_AdminAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MatKhauHash",
                table: "TaiKhoans",
                newName: "MatKhau");

            migrationBuilder.RenameColumn(
                name: "HoTen",
                table: "TaiKhoans",
                newName: "VaiTro");

            migrationBuilder.AddColumn<string>(
                name: "TenHienThi",
                table: "TaiKhoans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianTao",
                table: "TaiKhoans",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                table: "TaiKhoans",
                columns: new[] { "Id", "IsActive", "MatKhau", "TenDangNhap", "TenHienThi", "ThoiGianTao", "VaiTro" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), true, "123456", "admin", "Quản trị viên", new DateTime(2025, 7, 1, 14, 59, 53, 573, DateTimeKind.Local).AddTicks(8394), "Admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "TaiKhoans",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DropColumn(
                name: "TenHienThi",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "ThoiGianTao",
                table: "TaiKhoans");

            migrationBuilder.RenameColumn(
                name: "VaiTro",
                table: "TaiKhoans",
                newName: "HoTen");

            migrationBuilder.RenameColumn(
                name: "MatKhau",
                table: "TaiKhoans",
                newName: "MatKhauHash");
        }
    }
}
