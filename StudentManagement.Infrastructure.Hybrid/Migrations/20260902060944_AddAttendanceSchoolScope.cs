using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceSchoolScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SchoolId",
                table: "Attendances",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Schools_SchoolId",
                table: "Attendances",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Schools_SchoolId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SchoolId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Attendances");
        }
    }
}
