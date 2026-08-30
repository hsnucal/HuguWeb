using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentMembershipScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMembershipDepartmentScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserMembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMembershipDepartmentScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMembershipDepartmentScopes_UserMemberships_UserMembersh~",
                        column: x => x.UserMembershipId,
                        principalTable: "UserMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMembershipDepartmentScopes_DepartmentId",
                table: "UserMembershipDepartmentScopes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMembershipDepartmentScopes_Membership_Department",
                table: "UserMembershipDepartmentScopes",
                columns: new[] { "UserMembershipId", "DepartmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMembershipDepartmentScopes");
        }
    }
}
