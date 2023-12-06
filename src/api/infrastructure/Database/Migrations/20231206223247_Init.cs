using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lists",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Url = table.Column<string>(type: "character varying(22)", maxLength: 22, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PricesRefresh",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EurToUsdExchangeRate = table.Column<double>(type: "double precision", nullable: false),
                    SteamPricesLastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Buff163PricesLastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricesRefresh", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemActions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    UnitPrice = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemActions_Lists_ListId",
                        column: x => x.ListId,
                        principalTable: "Lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ListId = table.Column<long>(type: "bigint", nullable: false),
                    SteamValue = table.Column<long>(type: "bigint", nullable: true),
                    BuffValue = table.Column<long>(type: "bigint", nullable: true),
                    ItemPriceRefreshId = table.Column<long>(type: "bigint", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    SteamPriceCentsUsd = table.Column<long>(type: "bigint", nullable: true),
                    Buff163PriceCentsUsd = table.Column<long>(type: "bigint", nullable: true),
                    ItemPriceRefreshId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prices_PricesRefresh_ItemPriceRefreshId",
                        column: x => x.ItemPriceRefreshId,
                        principalTable: "PricesRefresh",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemActions_ListId",
                table: "ItemActions",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_Lists_Url",
                table: "Lists",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListSnapshots_ItemPriceRefreshId",
                table: "ListSnapshots",
                column: "ItemPriceRefreshId");

            migrationBuilder.CreateIndex(
                name: "IX_ListSnapshots_ListId",
                table: "ListSnapshots",
                column: "ListId");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_ItemPriceRefreshId",
                table: "Prices",
                column: "ItemPriceRefreshId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemActions");

            migrationBuilder.DropTable(
                name: "ListSnapshots");

            migrationBuilder.DropTable(
                name: "Prices");

            migrationBuilder.DropTable(
                name: "Lists");

            migrationBuilder.DropTable(
                name: "PricesRefresh");
        }
    }
}
