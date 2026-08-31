using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddCopilotConversationBranching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveBranchId",
                table: "CopilotConversations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CopilotBranchTurns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotBranchTurns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotConversationBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ThreadId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentBranchId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BranchedFromUserMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BranchedFromVersionNumber = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotConversationBranches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotBranchTurns_UserId_ThreadId_BranchId_Position",
                table: "CopilotBranchTurns",
                columns: new[] { "UserId", "ThreadId", "BranchId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopilotBranchTurns_UserId_ThreadId_BranchId_UserMessageId",
                table: "CopilotBranchTurns",
                columns: new[] { "UserId", "ThreadId", "BranchId", "UserMessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversationBranches_UserId_ThreadId_BranchId",
                table: "CopilotConversationBranches",
                columns: new[] { "UserId", "ThreadId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversationBranches_UserId_ThreadId_ParentBranchId",
                table: "CopilotConversationBranches",
                columns: new[] { "UserId", "ThreadId", "ParentBranchId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotBranchTurns");

            migrationBuilder.DropTable(
                name: "CopilotConversationBranches");

            migrationBuilder.DropColumn(
                name: "ActiveBranchId",
                table: "CopilotConversations");
        }
    }
}
