using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926100000_OrderApprovalAndIntendedStay")]
    public class OrderApprovalAndIntendedStay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IntendedStayStartsAt",
                table: "CartItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IntendedStayEndsAt",
                table: "CartItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IntendedStayStartsAt",
                table: "OrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IntendedStayEndsAt",
                table: "OrderItems",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IntendedStayStartsAt", table: "CartItems");
            migrationBuilder.DropColumn(name: "IntendedStayEndsAt", table: "CartItems");
            migrationBuilder.DropColumn(name: "IntendedStayStartsAt", table: "OrderItems");
            migrationBuilder.DropColumn(name: "IntendedStayEndsAt", table: "OrderItems");
        }
    }
}
