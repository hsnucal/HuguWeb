using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveRequestApprovalFoundationHr05B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceLeaveRequestId",
                table: "LeaveRecords",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApprovalStage = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.CheckConstraint("CK_LeaveRequests_Period", "\"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("CK_LeaveRequests_RequestedAmount", "\"RequestedAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequestDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DecisionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequestDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequestDecisions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRecords_SourceLeaveRequestId",
                table: "LeaveRecords",
                column: "SourceLeaveRequestId",
                unique: true,
                filter: "\"SourceLeaveRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequestDecisions_LeaveRequestId_DecisionAtUtc",
                table: "LeaveRequestDecisions",
                columns: new[] { "LeaveRequestId", "DecisionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_AssignmentId",
                table: "LeaveRequests",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmploymentId_ApprovalStage_Status",
                table: "LeaveRequests",
                columns: new[] { "EmploymentId", "ApprovalStage", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_EmploymentId_Status_StartDate_EndDate",
                table: "LeaveRequests",
                columns: new[] { "EmploymentId", "Status", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRecords_LeaveRequests_SourceLeaveRequestId",
                table: "LeaveRecords",
                column: "SourceLeaveRequestId",
                principalTable: "LeaveRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRecords_LeaveRequests_SourceLeaveRequestId",
                table: "LeaveRecords");

            migrationBuilder.DropTable(
                name: "LeaveRequestDecisions");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRecords_SourceLeaveRequestId",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "SourceLeaveRequestId",
                table: "LeaveRecords");
        }
    }
}
