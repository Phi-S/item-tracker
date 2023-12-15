using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ListSnapshotAndCurrencyStringLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuffValue",
                table: "ListSnapshots");

            migrationBuilder.DropColumn(
                name: "SteamValue",
                table: "ListSnapshots");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Lists",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BuffValue",
                table: "ListSnapshots",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SteamValue",
                table: "ListSnapshots",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Lists",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(5)",
                oldMaxLength: 5);
        }
    }
}
