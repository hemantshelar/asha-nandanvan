using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261003010000_DropClinicTermsLine")]
    public class DropClinicTermsLine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [DogSittingSettings]
SET [TermsAndConditions] = LTRIM(RTRIM(REPLACE([TermsAndConditions], 'We are a backyard, not a clinic.', '')))
WHERE [TermsAndConditions] LIKE '%We are a backyard, not a clinic.%';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
