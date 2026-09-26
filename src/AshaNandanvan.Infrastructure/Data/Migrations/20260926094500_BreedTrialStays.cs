using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926094500_BreedTrialStays")]
    public class BreedTrialStays : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLargeBreed",
                table: "DogBreeds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrialStay",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTrialStay",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE [DogBreeds] SET [OffersSitting] = 1
                WHERE [Name] IN (N'Alaskan Malamute', N'Bernese Mountain Dog', N'Giant Schnauzer', N'Rhodesian Ridgeback');

                UPDATE [DogBreeds] SET [IsLargeBreed] = 1
                WHERE [Name] IN (
                    N'Alaskan Malamute', N'Belgian Shepherd', N'Bernese Mountain Dog', N'Bloodhound',
                    N'Boxer', N'German Shepherd', N'Giant Schnauzer', N'Golden Retriever', N'Greyhound',
                    N'Labrador Retriever', N'Old English Sheepdog', N'Rhodesian Ridgeback', N'Samoyed',
                    N'Siberian Husky', N'Weimaraner', N'White Swiss Shepherd'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsLargeBreed", table: "DogBreeds");
            migrationBuilder.DropColumn(name: "IsTrialStay", table: "CartItems");
            migrationBuilder.DropColumn(name: "IsTrialStay", table: "OrderItems");
        }
    }
}
