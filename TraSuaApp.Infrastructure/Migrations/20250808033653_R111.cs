using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class R111 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ngay",
                table: "CongViecNoiBos");

            migrationBuilder.DropColumn(
                name: "ThoiGianTao",
                table: "CongViecNoiBos");

            migrationBuilder.RenameColumn(
                name: "ThoiGianHoanThanh",
                table: "CongViecNoiBos",
                newName: "NgayGio");

            migrationBuilder.RenameColumn(
                name: "NoiDung",
                table: "CongViecNoiBos",
                newName: "Ten");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "CongViecNoiBos",
                newName: "NoiDung");

            migrationBuilder.RenameColumn(
                name: "NgayGio",
                table: "CongViecNoiBos",
                newName: "ThoiGianHoanThanh");

            migrationBuilder.AddColumn<DateTime>(
                name: "Ngay",
                table: "CongViecNoiBos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ThoiGianTao",
                table: "CongViecNoiBos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
