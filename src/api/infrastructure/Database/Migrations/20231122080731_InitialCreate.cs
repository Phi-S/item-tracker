using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemLists",
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
                    table.PrimaryKey("PK_ItemLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    SteamPriceUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    SteamPriceEur = table.Column<decimal>(type: "numeric", nullable: true),
                    BuffPriceUsd = table.Column<decimal>(type: "numeric", nullable: true),
                    BuffPriceEur = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemListItemAction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemListDbModelId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    PricePerOne = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemListItemAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemListItemAction_ItemLists_ItemListDbModelId",
                        column: x => x.ItemListDbModelId,
                        principalTable: "ItemLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemListValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemListDbModelId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemListValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemListValues_ItemLists_ItemListDbModelId",
                        column: x => x.ItemListDbModelId,
                        principalTable: "ItemLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemListItemAction_ItemListDbModelId",
                table: "ItemListItemAction",
                column: "ItemListDbModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemLists_Url",
                table: "ItemLists",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemListValues_ItemListDbModelId",
                table: "ItemListValues",
                column: "ItemListDbModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemListItemAction");

            migrationBuilder.DropTable(
                name: "ItemListValues");

            migrationBuilder.DropTable(
                name: "ItemPrices");

            migrationBuilder.DropTable(
                name: "ItemLists");
        }
    }
}
