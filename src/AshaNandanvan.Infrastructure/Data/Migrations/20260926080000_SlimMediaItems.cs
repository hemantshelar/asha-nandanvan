using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlimMediaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaItems_OfferSlug_SortOrder",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "MediaItems");

            migrationBuilder.AlterColumn<string>(
                name: "YouTubeVideoId",
                table: "MediaItems",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_IsPublished_OfferSlug_SortOrder",
                table: "MediaItems",
                columns: new[] { "IsPublished", "OfferSlug", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_OfferSlug_YouTubeVideoId",
                table: "MediaItems",
                columns: new[] { "OfferSlug", "YouTubeVideoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaItems_IsPublished_OfferSlug_SortOrder",
                table: "MediaItems");

            migrationBuilder.DropIndex(
                name: "IX_MediaItems_OfferSlug_YouTubeVideoId",
                table: "MediaItems");

            migrationBuilder.AlterColumn<string>(
                name: "YouTubeVideoId",
                table: "MediaItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "MediaItems",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero));

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "MediaItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_OfferSlug_SortOrder",
                table: "MediaItems",
                columns: new[] { "OfferSlug", "SortOrder" });
        }
    }
}
