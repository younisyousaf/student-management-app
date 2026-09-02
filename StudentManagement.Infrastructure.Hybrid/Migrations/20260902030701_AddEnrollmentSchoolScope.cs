using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentSchoolScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SchoolId",
                table: "Enrollments",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Schools_SchoolId",
                table: "Enrollments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Schools_SchoolId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SchoolId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Enrollments");
        }
    }
}
