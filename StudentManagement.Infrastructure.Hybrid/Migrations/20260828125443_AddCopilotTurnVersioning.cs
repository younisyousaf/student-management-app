using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddCopilotTurnVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersionNumber",
                table: "CopilotTurns",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CopilotTurnVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    UserContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssistantMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssistantContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActivitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotTurnVersions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotTurnVersions_UserId_ThreadId_UserMessageId",
                table: "CopilotTurnVersions",
                columns: new[] { "UserId", "ThreadId", "UserMessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotTurnVersions_UserId_ThreadId_UserMessageId_VersionNumber",
                table: "CopilotTurnVersions",
                columns: new[] { "UserId", "ThreadId", "UserMessageId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotTurnVersions");

            migrationBuilder.DropColumn(
                name: "CurrentVersionNumber",
                table: "CopilotTurns");
        }
    }
}
