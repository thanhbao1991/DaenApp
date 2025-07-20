using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedNhomSanPhamSTT2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GioiTinh",
                table: "KhachHangs");

            migrationBuilder.DropColumn(
                name: "NgaySinh",
                table: "KhachHangs");

            migrationBuilder.AddColumn<int>(
                name: "OldId",
                table: "KhachHangs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldId",
                table: "KhachHangs");

            migrationBuilder.AddColumn<string>(
                name: "GioiTinh",
                table: "KhachHangs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgaySinh",
                table: "KhachHangs",
                type: "datetime2",
                nullable: true);
        }
    }
}
