using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DogSittingPetName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PetName",
                table: "OrderItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetName",
                table: "CartItems",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PetName",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PetName",
                table: "CartItems");
        }
    }
}
