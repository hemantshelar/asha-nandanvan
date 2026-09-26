using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260926080000_SlimMediaItems")]
    public class SlimMediaItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_OfferSlug_SortOrder' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    DROP INDEX [IX_MediaItems_OfferSlug_SortOrder] ON [MediaItems];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_IsPublished_OfferSlug_SortOrder' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    CREATE INDEX [IX_MediaItems_IsPublished_OfferSlug_SortOrder]
                        ON [MediaItems] ([IsPublished], [OfferSlug], [SortOrder]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_OfferSlug_YouTubeVideoId' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    CREATE UNIQUE INDEX [IX_MediaItems_OfferSlug_YouTubeVideoId]
                        ON [MediaItems] ([OfferSlug], [YouTubeVideoId]);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_IsPublished_OfferSlug_SortOrder' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    DROP INDEX [IX_MediaItems_IsPublished_OfferSlug_SortOrder] ON [MediaItems];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_OfferSlug_YouTubeVideoId' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    DROP INDEX [IX_MediaItems_OfferSlug_YouTubeVideoId] ON [MediaItems];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MediaItems_OfferSlug_SortOrder' AND object_id = OBJECT_ID(N'dbo.MediaItems'))
                    CREATE INDEX [IX_MediaItems_OfferSlug_SortOrder]
                        ON [MediaItems] ([OfferSlug], [SortOrder]);
                """);
        }
    }
}
