using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926083000_RestoreMediaColumns")]
    public class RestoreMediaColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.MediaItems', 'SourceUrl') IS NULL
                    ALTER TABLE [MediaItems] ADD [SourceUrl] nvarchar(500) NOT NULL CONSTRAINT [DF_MediaItems_SourceUrl] DEFAULT(N'');
                IF COL_LENGTH('dbo.MediaItems', 'CreatedAt') IS NULL
                    ALTER TABLE [MediaItems] ADD [CreatedAt] datetimeoffset NOT NULL CONSTRAINT [DF_MediaItems_CreatedAt] DEFAULT('0001-01-01T00:00:00+00:00');
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'dbo.MediaItems') AND name = N'YouTubeVideoId' AND max_length = 22)
                    ALTER TABLE [MediaItems] ALTER COLUMN [YouTubeVideoId] nvarchar(20) NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
