using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.RoomOperations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialRoomOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentReadiness = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReadinessCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadinessVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                    table.CheckConstraint("CK_Rooms_Readiness", "\"CurrentReadiness\" IN ('Dirty', 'Clean', 'Inspected', 'Ready')");
                });

            migrationBuilder.CreateTable(
                name: "HousekeepingWorkItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadinessCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceInspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousekeepingWorkItems", x => x.Id);
                    table.CheckConstraint("CK_HousekeepingWorkItems_Priority", "\"Priority\" IN ('Normal', 'High', 'Urgent')");
                    table.CheckConstraint("CK_HousekeepingWorkItems_State", "\"State\" IN ('Open', 'Completed')");
                    table.ForeignKey(
                        name: "FK_HousekeepingWorkItems_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomInspections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadinessCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomInspections", x => x.Id);
                    table.CheckConstraint("CK_RoomInspections_Result", "\"Result\" IN ('Accepted', 'Rejected')");
                    table.CheckConstraint("CK_RoomInspections_RejectedHasReason", "\"Result\" <> 'Rejected' OR (\"Reason\" IS NOT NULL AND btrim(\"Reason\") <> '')");
                    table.ForeignKey(
                        name: "FK_RoomInspections_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoomReadinessHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadinessCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Readiness = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Cause = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomReadinessHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomReadinessHistory_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PropertyId",
                table: "Rooms",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_PropertyId_Number",
                table: "Rooms",
                columns: new[] { "PropertyId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HousekeepingWorkItems_RoomId",
                table: "HousekeepingWorkItems",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_HousekeepingWorkItems_RoomId_Open",
                table: "HousekeepingWorkItems",
                columns: new[] { "RoomId", "State" },
                unique: true,
                filter: "\"State\" = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_RoomInspections_RoomId_OccurredAt",
                table: "RoomInspections",
                columns: new[] { "RoomId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomReadinessHistory_RoomId_OccurredAt",
                table: "RoomReadinessHistory",
                columns: new[] { "RoomId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HousekeepingWorkItems");
            migrationBuilder.DropTable(name: "RoomInspections");
            migrationBuilder.DropTable(name: "RoomReadinessHistory");
            migrationBuilder.DropTable(name: "Rooms");
        }
    }
}
