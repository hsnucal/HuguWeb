using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.TechnicalService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTechnicalService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceIssueCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceIssueCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    OriginNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlocksRoomUse = table.Column<bool>(type: "boolean", nullable: false),
                    OutageClassification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UnableToResolveNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreparationImpact = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceIssues", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceIssues_InProgressHasAssignee", "\"Status\" <> 'InProgress' OR \"AssignedEmployeeId\" IS NOT NULL");
                    table.CheckConstraint("CK_MaintenanceIssues_Outage", "(\"BlocksRoomUse\" = FALSE AND \"OutageClassification\" IS NULL) OR (\"BlocksRoomUse\" = TRUE AND \"OutageClassification\" IN ('OutOfOrder', 'OutOfService'))");
                    table.CheckConstraint("CK_MaintenanceIssues_Priority", "\"Priority\" IN ('Normal', 'High', 'Urgent')");
                    table.CheckConstraint("CK_MaintenanceIssues_ResolvedHasNote", "\"Status\" <> 'Resolved' OR (\"ResolutionNote\" IS NOT NULL AND btrim(\"ResolutionNote\") <> '')");
                    table.CheckConstraint("CK_MaintenanceIssues_Status", "\"Status\" IN ('Open', 'InProgress', 'UnableToResolve', 'Resolved')");
                    table.CheckConstraint("CK_MaintenanceIssues_UnableHasNote", "\"Status\" <> 'UnableToResolve' OR (\"UnableToResolveNote\" IS NOT NULL AND btrim(\"UnableToResolveNote\") <> '')");
                    table.ForeignKey(
                        name: "FK_MaintenanceIssues_MaintenanceIssueCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MaintenanceIssueCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceIssueHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FromEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromPriority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ToPriority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BlocksRoomUse = table.Column<bool>(type: "boolean", nullable: true),
                    OutageClassification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PreparationImpact = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceIssueHistory", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceIssueHistory_Event", "\"EventType\" IN ('Created', 'Assigned', 'Reassigned', 'PriorityChanged', 'BlockingChanged', 'Started', 'UnableToResolve', 'Resumed', 'Resolved')");
                    table.ForeignKey(
                        name: "FK_MaintenanceIssueHistory_MaintenanceIssues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "MaintenanceIssues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssueCategories_PropertyId",
                table: "MaintenanceIssueCategories",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssueCategories_PropertyId_Name",
                table: "MaintenanceIssueCategories",
                columns: new[] { "PropertyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssueHistory_IssueId_OccurredAt",
                table: "MaintenanceIssueHistory",
                columns: new[] { "IssueId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssues_AssignedEmployeeId",
                table: "MaintenanceIssues",
                column: "AssignedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssues_CategoryId",
                table: "MaintenanceIssues",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssues_CreatedAt",
                table: "MaintenanceIssues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssues_PropertyId_Status",
                table: "MaintenanceIssues",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceIssues_RoomId_Status",
                table: "MaintenanceIssues",
                columns: new[] { "RoomId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceIssueHistory");

            migrationBuilder.DropTable(
                name: "MaintenanceIssues");

            migrationBuilder.DropTable(
                name: "MaintenanceIssueCategories");
        }
    }
}
