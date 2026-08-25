using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Api.Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationMembershipModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorizationAuditRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ActorOrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorPropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    PermissionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthorizationRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorizationRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAccountLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAccountLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMemberships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionCode });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AuthorizationRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AuthorizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_AuthorizationRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AuthorizationRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignments_UserMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "UserMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditRecords_OccurredAtUtc",
                table: "AuthorizationAuditRecords",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditRecords_ActorUserId",
                table: "AuthorizationAuditRecords",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationAuditRecords_ActorOrganizationId",
                table: "AuthorizationAuditRecords",
                column: "ActorOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorizationRoles_OrganizationId_Code",
                table: "AuthorizationRoles",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountLinks_EmployeeId",
                table: "EmployeeAccountLinks",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountLinks_UserId",
                table: "EmployeeAccountLinks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMemberships_User_Organization_OrgWide",
                table: "UserMemberships",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true,
                filter: "\"PropertyId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMemberships_User_Organization_Property",
                table: "UserMemberships",
                columns: new[] { "UserId", "OrganizationId", "PropertyId" },
                unique: true,
                filter: "\"PropertyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserMemberships_UserId",
                table: "UserMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMemberships_OrganizationId",
                table: "UserMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMemberships_PropertyId",
                table: "UserMemberships",
                column: "PropertyId",
                filter: "\"PropertyId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_MembershipId_RoleId",
                table: "UserRoleAssignments",
                columns: new[] { "MembershipId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignments_RoleId",
                table: "UserRoleAssignments",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorizationAuditRecords");

            migrationBuilder.DropTable(
                name: "EmployeeAccountLinks");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoleAssignments");

            migrationBuilder.DropTable(
                name: "AuthorizationRoles");

            migrationBuilder.DropTable(
                name: "UserMemberships");
        }
    }
}
