using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNgayCanhBao_XNgayCanhBao_To_CongViecNoiBo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayCanhBao",
                table: "CongViecNoiBos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XNgayCanhBao",
                table: "CongViecNoiBos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NgayCanhBao",
                table: "CongViecNoiBos");

            migrationBuilder.DropColumn(
                name: "XNgayCanhBao",
                table: "CongViecNoiBos");
        }
    }
}
