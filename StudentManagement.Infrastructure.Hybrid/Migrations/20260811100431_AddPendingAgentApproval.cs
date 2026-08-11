using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingAgentApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalArgumentsJson",
                table: "AgentSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalCallId",
                table: "AgentSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalFunctionName",
                table: "AgentSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingApprovalRequestId",
                table: "AgentSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingApprovalArgumentsJson",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "PendingApprovalCallId",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "PendingApprovalFunctionName",
                table: "AgentSessions");

            migrationBuilder.DropColumn(
                name: "PendingApprovalRequestId",
                table: "AgentSessions");
        }
    }
}
