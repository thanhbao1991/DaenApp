using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraSuaApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEntityIdToLogdđffDđcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TonKho",
                table: "NguyenLieus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TonKho",
                table: "NguyenLieus",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
