using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926020000_DogSittingStays")]
    public class DogSittingStays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StayStartsAt",
                table: "CartItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StayEndsAt",
                table: "CartItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StayStartsAt",
                table: "OrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StayEndsAt",
                table: "OrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductSlotId",
                table: "OrderItems",
                column: "ProductSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductSlots_ProductSlotId",
                table: "OrderItems",
                column: "ProductSlotId",
                principalTable: "ProductSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateTable(
                name: "DogSittingSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxDogs = table.Column<int>(type: "int", nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TermsAndConditions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogSittingSettings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductSlots_ProductSlotId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductSlotId",
                table: "OrderItems");

            migrationBuilder.DropTable(name: "DogSittingSettings");
            migrationBuilder.DropColumn(name: "StayStartsAt", table: "CartItems");
            migrationBuilder.DropColumn(name: "StayEndsAt", table: "CartItems");
            migrationBuilder.DropColumn(name: "StayStartsAt", table: "OrderItems");
            migrationBuilder.DropColumn(name: "StayEndsAt", table: "OrderItems");
        }
    }
}
