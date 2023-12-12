using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class PriceRefreshExchangeRateRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EurToUsdExchangeRate",
                table: "PricesRefresh",
                newName: "UsdToEurExchangeRate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UsdToEurExchangeRate",
                table: "PricesRefresh",
                newName: "EurToUsdExchangeRate");
        }
    }
}
