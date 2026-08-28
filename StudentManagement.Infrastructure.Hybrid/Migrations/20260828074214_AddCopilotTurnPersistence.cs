using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddCopilotTurnPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CopilotTurns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActivitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotTurns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotTurns_UserId_ThreadId_CreatedAt",
                table: "CopilotTurns",
                columns: new[] { "UserId", "ThreadId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotTurns_UserId_ThreadId_UserMessageId",
                table: "CopilotTurns",
                columns: new[] { "UserId", "ThreadId", "UserMessageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotTurns");
        }
    }
}
