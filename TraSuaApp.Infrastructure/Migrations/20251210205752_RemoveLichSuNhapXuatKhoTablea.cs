using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLichSuNhapXuatKhoTablea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "CongThucs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Loai",
                table: "CongThucs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ten",
                table: "CongThucs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "CongThucs");

            migrationBuilder.DropColumn(
                name: "Loai",
                table: "CongThucs");

            migrationBuilder.DropColumn(
                name: "Ten",
                table: "CongThucs");
        }
    }
}
