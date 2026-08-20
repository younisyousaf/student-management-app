using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowApprovalDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "EnrollmentWorkflowRecords",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approved",
                table: "EnrollmentWorkflowRecords");
        }
    }
}
