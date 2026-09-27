using System;
using AshaNandanvan.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260928080000_StayAlbums")]
    public class StayAlbums : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StayAlbums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TitleIsDefault = table.Column<bool>(type: "bit", nullable: false),
                    InviteToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayAlbums_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StayAlbumClips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    YouTubeVideoId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    FilmedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayAlbumClips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayAlbumClips_StayAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "StayAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StayAlbumMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayAlbumMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayAlbumMembers_StayAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "StayAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StayAlbumComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClipId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayAlbumComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayAlbumComments_StayAlbumClips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "StayAlbumClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StayAlbumReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClipId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StayAlbumReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StayAlbumReactions_StayAlbumClips_ClipId",
                        column: x => x.ClipId,
                        principalTable: "StayAlbumClips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbums_InviteToken",
                table: "StayAlbums",
                column: "InviteToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbums_OrderId",
                table: "StayAlbums",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbumClips_AlbumId_SortOrder",
                table: "StayAlbumClips",
                columns: new[] { "AlbumId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbumClips_AlbumId_YouTubeVideoId",
                table: "StayAlbumClips",
                columns: new[] { "AlbumId", "YouTubeVideoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbumMembers_AlbumId_UserId",
                table: "StayAlbumMembers",
                columns: new[] { "AlbumId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbumComments_ClipId_CreatedAt",
                table: "StayAlbumComments",
                columns: new[] { "ClipId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StayAlbumReactions_ClipId_UserId",
                table: "StayAlbumReactions",
                columns: new[] { "ClipId", "UserId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StayAlbumComments");
            migrationBuilder.DropTable(name: "StayAlbumMembers");
            migrationBuilder.DropTable(name: "StayAlbumReactions");
            migrationBuilder.DropTable(name: "StayAlbumClips");
            migrationBuilder.DropTable(name: "StayAlbums");
        }
    }
}
