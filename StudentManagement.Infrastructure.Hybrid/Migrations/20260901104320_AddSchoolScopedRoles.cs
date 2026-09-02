using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Infrastructure.Hybrid.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolUserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolMembershipId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolUserRoles_IdentityRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "IdentityRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolUserRoles_IdentityUsers_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "IdentityUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolUserRoles_SchoolMemberships_SchoolMembershipId",
                        column: x => x.SchoolMembershipId,
                        principalTable: "SchoolMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolUserRoles_AssignedByUserId",
                table: "SchoolUserRoles",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolUserRoles_RoleId",
                table: "SchoolUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolUserRoles_SchoolMembershipId_RoleId",
                table: "SchoolUserRoles",
                columns: new[] { "SchoolMembershipId", "RoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolUserRoles");
        }
    }
}
