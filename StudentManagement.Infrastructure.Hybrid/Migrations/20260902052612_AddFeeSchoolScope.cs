using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeSchoolScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SchoolId",
                table: "Fees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fees_SchoolId",
                table: "Fees",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fees_Schools_SchoolId",
                table: "Fees",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fees_Schools_SchoolId",
                table: "Fees");

            migrationBuilder.DropIndex(
                name: "IX_Fees_SchoolId",
                table: "Fees");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Fees");
        }
    }
}
