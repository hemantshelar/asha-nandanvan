using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260929090000_StayRatePlans")]
    public class StayRatePlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StayRatePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FirstDogPerNight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ExtraDogPerNight = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MinNights = table.Column<int>(type: "int", nullable: false),
                    MaxNights = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayRatePlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StayRatePlans_Code",
                table: "StayRatePlans",
                column: "Code",
                unique: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedStayPlanId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AssignedStayPlanId",
                table: "AspNetUsers",
                column: "AssignedStayPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_StayRatePlans_AssignedStayPlanId",
                table: "AspNetUsers",
                column: "AssignedStayPlanId",
                principalTable: "StayRatePlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddColumn<Guid>(
                name: "StayGroupId",
                table: "CartItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StayPlanName",
                table: "CartItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StayNightlyRate",
                table: "CartItems",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompanionDog",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true,
                filter: "[ProductSlotId] IS NULL AND [StayStartsAt] IS NULL");

            migrationBuilder.AddColumn<Guid>(
                name: "StayGroupId",
                table: "OrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StayPlanName",
                table: "OrderItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StayNightlyRate",
                table: "OrderItems",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompanionDog",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_StayRatePlans_AssignedStayPlanId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AssignedStayPlanId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(name: "AssignedStayPlanId", table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems");

            migrationBuilder.DropColumn(name: "StayGroupId", table: "CartItems");
            migrationBuilder.DropColumn(name: "StayPlanName", table: "CartItems");
            migrationBuilder.DropColumn(name: "StayNightlyRate", table: "CartItems");
            migrationBuilder.DropColumn(name: "IsCompanionDog", table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true,
                filter: "[ProductSlotId] IS NULL");

            migrationBuilder.DropColumn(name: "StayGroupId", table: "OrderItems");
            migrationBuilder.DropColumn(name: "StayPlanName", table: "OrderItems");
            migrationBuilder.DropColumn(name: "StayNightlyRate", table: "OrderItems");
            migrationBuilder.DropColumn(name: "IsCompanionDog", table: "OrderItems");

            migrationBuilder.DropTable(name: "StayRatePlans");
        }
    }
}
