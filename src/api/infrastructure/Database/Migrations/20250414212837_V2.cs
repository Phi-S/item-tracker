using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Database.Migrations
{
    public record ItemModel(long Id, string AppId, string Name, string HashName, string Url, string Image);

    /// <inheritdoc />
    public partial class V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Buff163PricesLastModified",
                table: "PricesRefresh");

            migrationBuilder.DropColumn(
                name: "Buff163PriceCentsUsd",
                table: "Prices");
            
            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "Prices",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                table: "ItemActions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
            
            migrationBuilder.CreateIndex(
                name: "IX_Prices_ItemId",
                table: "Prices",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_ItemName",
                table: "Prices",
                column: "ItemName");

            migrationBuilder.CreateIndex(
                name: "IX_ItemActions_ItemId",
                table: "ItemActions",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemActions_ItemName",
                table: "ItemActions",
                column: "ItemName");
            
            var itemsJsonStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("infrastructure.Database.Migrations.20250414212837_V2_items.json");
            if (itemsJsonStream is null)
            {
                throw new Exception("Failed to get \"infrastructure.Database.Migrations.20250414212837_V2_items.json\" file");
            }

            using var reader = new StreamReader(itemsJsonStream, Encoding.UTF8);
            var itemsJson = reader.ReadToEnd();
            var itemList = JsonSerializer.Deserialize<List<ItemModel>>(itemsJson);

            var sql = new StringBuilder();
            var counter = 0;
            foreach (var item in itemList)
            { 
                var itemName = item.Name.Replace("'", "''");
                sql.Append($"""UPDATE "Prices" SET "ItemName" = '{itemName}' WHERE "ItemId" = {item.Id};""");
                sql.Append($"""UPDATE "ItemActions" SET "ItemName" = '{itemName}' WHERE "ItemId" = {item.Id};""");

                counter++;

                if (counter % 200 == 0)
                {
                    migrationBuilder.Sql(sql.ToString());
                    counter = 0;
                    sql.Clear();
                }
            }
            migrationBuilder.Sql(sql.ToString());

            migrationBuilder.Sql("""DELETE FROM "Prices" WHERE "ItemName" = '';""");
            migrationBuilder.Sql("""DELETE FROM "ItemActions" WHERE "ItemName" = '';""");
            
            migrationBuilder.DropIndex(
                name: "IX_Prices_ItemId",
                table: "Prices");

            migrationBuilder.DropIndex(
                name: "IX_ItemActions_ItemId",
                table: "ItemActions");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Prices");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "ItemActions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new Exception("can not downgrade from V2");
        }
    }
}
