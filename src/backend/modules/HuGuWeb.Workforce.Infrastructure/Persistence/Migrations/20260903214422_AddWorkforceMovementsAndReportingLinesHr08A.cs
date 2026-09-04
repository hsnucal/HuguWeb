using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkforceMovementsAndReportingLinesHr08A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkforceReportingLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubordinateEmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerEmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceReportingLines", x => x.Id);
                    table.CheckConstraint("CK_WorkforceReportingLines_Period", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
                    table.ForeignKey(
                        name: "FK_WorkforceReportingLines_Employments_ManagerEmploymentId",
                        column: x => x.ManagerEmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkforceReportingLines_Employments_SubordinateEmploymentId",
                        column: x => x.SubordinateEmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkforceReportingLines_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreviousAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewAssignmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousReportingLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewReportingLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CancelledByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_Assignments_NewAssignmentId",
                        column: x => x.NewAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_Assignments_PreviousAssignmentId",
                        column: x => x.PreviousAssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_WorkforceReportingLines_NewReportingLine~",
                        column: x => x.NewReportingLineId,
                        principalTable: "WorkforceReportingLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelMovements_WorkforceReportingLines_PreviousReportin~",
                        column: x => x.PreviousReportingLineId,
                        principalTable: "WorkforceReportingLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_EmploymentId",
                table: "PersonnelMovements",
                column: "EmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_NewAssignmentId",
                table: "PersonnelMovements",
                column: "NewAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_NewReportingLineId",
                table: "PersonnelMovements",
                column: "NewReportingLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_OrganizationId_EmploymentId_EffectiveDate",
                table: "PersonnelMovements",
                columns: new[] { "OrganizationId", "EmploymentId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_OrganizationId_MovementType_EffectiveDate",
                table: "PersonnelMovements",
                columns: new[] { "OrganizationId", "MovementType", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_PreviousAssignmentId",
                table: "PersonnelMovements",
                column: "PreviousAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelMovements_PreviousReportingLineId",
                table: "PersonnelMovements",
                column: "PreviousReportingLineId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceReportingLines_ManagerEmploymentId_EffectiveFrom",
                table: "WorkforceReportingLines",
                columns: new[] { "ManagerEmploymentId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceReportingLines_OrganizationId",
                table: "WorkforceReportingLines",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceReportingLines_SubordinateEmploymentId_EffectiveFrom_EffectiveTo",
                table: "WorkforceReportingLines",
                columns: new[] { "SubordinateEmploymentId", "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonnelMovements");

            migrationBuilder.DropTable(
                name: "WorkforceReportingLines");
        }
    }
}
