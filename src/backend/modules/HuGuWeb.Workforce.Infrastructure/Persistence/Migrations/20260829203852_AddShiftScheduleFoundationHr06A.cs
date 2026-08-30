using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftScheduleFoundationHr06A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleEntryChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduleEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreviousKind = table.Column<int>(type: "integer", nullable: true),
                    PreviousShiftDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewKind = table.Column<int>(type: "integer", nullable: true),
                    NewShiftDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEntryChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleEntryChanges_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartLocalTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndLocalTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndsNextDay = table.Column<bool>(type: "boolean", nullable: false),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftDefinitions", x => x.Id);
                    table.CheckConstraint("CK_ShiftDefinitions_BreakMinutes", "\"BreakMinutes\" >= 0");
                    table.ForeignKey(
                        name: "FK_ShiftDefinitions_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduleDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ShiftDefinitionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEntries", x => x.Id);
                    table.CheckConstraint("CK_ScheduleEntries_KindShiftDefinition", "(\"Kind\" = 1 AND \"ShiftDefinitionId\" IS NOT NULL) OR (\"Kind\" = 2 AND \"ShiftDefinitionId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_ShiftDefinitions_ShiftDefinitionId",
                        column: x => x.ShiftDefinitionId,
                        principalTable: "ShiftDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_AssignmentId",
                table: "ScheduleEntries",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_EmploymentId_ScheduleDate",
                table: "ScheduleEntries",
                columns: new[] { "EmploymentId", "ScheduleDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ShiftDefinitionId",
                table: "ScheduleEntries",
                column: "ShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntryChanges_EmploymentId_ScheduleDate_ChangedAtUtc",
                table: "ScheduleEntryChanges",
                columns: new[] { "EmploymentId", "ScheduleDate", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntryChanges_NewShiftDefinitionId",
                table: "ScheduleEntryChanges",
                column: "NewShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntryChanges_PreviousShiftDefinitionId",
                table: "ScheduleEntryChanges",
                column: "PreviousShiftDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDefinitions_PropertyId_Code",
                table: "ShiftDefinitions",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDefinitions_PropertyId_IsActive",
                table: "ShiftDefinitions",
                columns: new[] { "PropertyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleEntries");

            migrationBuilder.DropTable(
                name: "ScheduleEntryChanges");

            migrationBuilder.DropTable(
                name: "ShiftDefinitions");
        }
    }
}
