using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AshaNandanvan.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class VisitTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VisitSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PageViews = table.Column<int>(type: "int", nullable: false),
                    IsFirstVisit = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FirstTouchSource = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Campaign = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Medium = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Referrer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LandingPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExitPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IpHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IpNetwork = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Device = table.Column<int>(type: "int", nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BrowserVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PlatformVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DeviceModel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ViewportWidth = table.Column<int>(type: "int", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsBot = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitSessionId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Value = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitEvents_VisitSessions_VisitSessionId",
                        column: x => x.VisitSessionId,
                        principalTable: "VisitSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitEvents_Kind_At",
                table: "VisitEvents",
                columns: new[] { "Kind", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitEvents_VisitSessionId_Kind_At",
                table: "VisitEvents",
                columns: new[] { "VisitSessionId", "Kind", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_IsBot_StartedAt",
                table: "VisitSessions",
                columns: new[] { "IsBot", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_SessionKey",
                table: "VisitSessions",
                column: "SessionKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitSessions_VisitorId_LastSeenAt",
                table: "VisitSessions",
                columns: new[] { "VisitorId", "LastSeenAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitEvents");

            migrationBuilder.DropTable(
                name: "VisitSessions");
        }
    }
}
