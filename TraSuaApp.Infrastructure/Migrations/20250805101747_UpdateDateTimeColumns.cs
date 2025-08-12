using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateTimeColumns : Migration
    {

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
           name: "ThoiGian",
           table: "ChiTietHoaDonPoints");

            migrationBuilder.AddColumn<DateTime>(
                name: "Ngay",
                table: "ChiTietHoaDonPoints",
                type: "date",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayGio",
                table: "ChiTietHoaDonPoints",
                type: "datetime",
                nullable: false,
                defaultValueSql: "GETDATE()");
        }

        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
