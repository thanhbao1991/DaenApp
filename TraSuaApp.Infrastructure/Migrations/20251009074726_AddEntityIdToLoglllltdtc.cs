using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToLoglllltdtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TuDienTraCuus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuDienTraCuus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonThanhToans_TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans",
                column: "TuDienTraCuuId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_TuDienTraCuus_TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans",
                column: "TuDienTraCuuId",
                principalTable: "TuDienTraCuus",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_TuDienTraCuus_TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropTable(
                name: "TuDienTraCuus");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonThanhToans_TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropColumn(
                name: "TuDienTraCuuId",
                table: "ChiTietHoaDonThanhToans");
        }
    }
}
