using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    public partial class AddLoaiThanhToanAndNoRefToChiTietHoaDonThanhToan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
    name: "ChiTietHoaDonNoId",
    table: "ChiTietHoaDonThanhToans",
    type: "uniqueidentifier",
    nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiThanhToan",
                table: "ChiTietHoaDonThanhToans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietHoaDonThanhToans_ChiTietHoaDonNoId",
                table: "ChiTietHoaDonThanhToans",
                column: "ChiTietHoaDonNoId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_ChiTietHoaDonNos_ChiTietHoaDonNoId",
                table: "ChiTietHoaDonThanhToans",
                column: "ChiTietHoaDonNoId",
                principalTable: "ChiTietHoaDonNos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // Thay Cascade bằng Restrict
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietHoaDonThanhToans_ChiTietHoaDonNos_ChiTietHoaDonNoId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietHoaDonThanhToans_ChiTietHoaDonNoId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropColumn(
                name: "ChiTietHoaDonNoId",
                table: "ChiTietHoaDonThanhToans");

            migrationBuilder.DropColumn(
                name: "LoaiThanhToan",
                table: "ChiTietHoaDonThanhToans");
        }
    }
}