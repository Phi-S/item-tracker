using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemovedListSnapshotTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListSnapshots");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemPriceRefreshId = table.Column<long>(type: "bigint", nullable: false),
                    ListId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListSnapshots_Lists_ListId",
                        column: x => x.ListId,
                        principalTable: "Lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListSnapshots_PricesRefresh_ItemPriceRefreshId",
                        column: x => x.ItemPriceRefreshId,
                        principalTable: "PricesRefresh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListSnapshots_ItemPriceRefreshId",
                table: "ListSnapshots",
                column: "ItemPriceRefreshId");

            migrationBuilder.CreateIndex(
                name: "IX_ListSnapshots_ListId",
                table: "ListSnapshots",
                column: "ListId");
        }
    }
}
