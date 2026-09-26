using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926004500_BookingSlots")]
    public class BookingSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    BookedCount = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSlots_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "ProductSlotId",
                table: "CartItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductSlotId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SlotLabel",
                table: "OrderItems",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true,
                filter: "[ProductSlotId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId_ProductSlotId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId", "ProductSlotId" },
                unique: true,
                filter: "[ProductSlotId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductSlotId",
                table: "CartItems",
                column: "ProductSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSlots_ProductId_StartsAt",
                table: "ProductSlots",
                columns: new[] { "ProductId", "StartsAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_ProductSlots_ProductSlotId",
                table: "CartItems",
                column: "ProductSlotId",
                principalTable: "ProductSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_ProductSlots_ProductSlotId",
                table: "CartItems");

            migrationBuilder.DropTable(
                name: "ProductSlots");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId_ProductSlotId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductSlotId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductSlotId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ProductSlotId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SlotLabel",
                table: "OrderItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);
        }
    }
}
