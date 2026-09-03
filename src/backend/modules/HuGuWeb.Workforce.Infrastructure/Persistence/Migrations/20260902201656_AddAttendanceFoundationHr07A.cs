using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceFoundationHr07A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceCorrectionChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CorrectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousKind = table.Column<int>(type: "integer", nullable: true),
                    PreviousReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NewKind = table.Column<int>(type: "integer", nullable: true),
                    NewReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCorrectionChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrectionChanges_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceCorrections_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrectionChanges_EmploymentId_LocalDate_ChangedAtUtc",
                table: "AttendanceCorrectionChanges",
                columns: new[] { "EmploymentId", "LocalDate", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_AssignmentId",
                table: "AttendanceCorrections",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_EmploymentId_LocalDate",
                table: "AttendanceCorrections",
                columns: new[] { "EmploymentId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_OrganizationId_PropertyId_EmploymentId_LocalDate",
                table: "AttendanceCorrections",
                columns: new[] { "OrganizationId", "PropertyId", "EmploymentId", "LocalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceCorrections_PropertyId",
                table: "AttendanceCorrections",
                column: "PropertyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceCorrectionChanges");

            migrationBuilder.DropTable(
                name: "AttendanceCorrections");
        }
    }
}
