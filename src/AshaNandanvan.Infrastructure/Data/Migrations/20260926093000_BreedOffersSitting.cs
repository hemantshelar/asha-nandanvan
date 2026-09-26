using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926093000_BreedOffersSitting")]
    public class BreedOffersSitting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OffersSitting",
                table: "DogBreeds",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("""
                UPDATE [DogBreeds] SET [OffersSitting] = 0
                WHERE [Name] IN (
                    N'Akita', N'Alaskan Malamute', N'American Bulldog', N'American Pit Bull Terrier',
                    N'American Staffordshire Terrier', N'Belgian Malinois', N'Bernese Mountain Dog',
                    N'Bullmastiff', N'Cane Corso', N'Dingo', N'Dobermann', N'Dogo Argentino',
                    N'Dogue de Bordeaux', N'Fila Brasileiro', N'Giant Schnauzer', N'Great Dane',
                    N'Irish Wolfhound', N'Japanese Tosa', N'Leonberger', N'Mastiff', N'Newfoundland',
                    N'Perro de Presa Canario', N'Pit Bull Terrier', N'Rhodesian Ridgeback',
                    N'Rottweiler', N'Saint Bernard'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OffersSitting", table: "DogBreeds");
        }
    }
}
