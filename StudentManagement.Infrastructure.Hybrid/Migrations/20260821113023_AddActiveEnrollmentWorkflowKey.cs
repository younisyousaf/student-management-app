using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveEnrollmentWorkflowKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveKey",
                table: "EnrollmentWorkflowRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentWorkflowRecords_ActiveKey",
                table: "EnrollmentWorkflowRecords",
                column: "ActiveKey",
                unique: true,
                filter: "[ActiveKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnrollmentWorkflowRecords_ActiveKey",
                table: "EnrollmentWorkflowRecords");

            migrationBuilder.DropColumn(
                name: "ActiveKey",
                table: "EnrollmentWorkflowRecords");
        }
    }
}
