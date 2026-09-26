using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926090000_DogBreeds")]
    public class DogBreeds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PetBreed",
                table: "OrderItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetBreed",
                table: "CartItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DogBreeds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DogBreeds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DogBreeds_Name",
                table: "DogBreeds",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DogBreeds_IsApproved_IsRejected_Name",
                table: "DogBreeds",
                columns: new[] { "IsApproved", "IsRejected", "Name" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DogBreeds");
            migrationBuilder.DropColumn(name: "PetBreed", table: "OrderItems");
            migrationBuilder.DropColumn(name: "PetBreed", table: "CartItems");
        }
    }
}
