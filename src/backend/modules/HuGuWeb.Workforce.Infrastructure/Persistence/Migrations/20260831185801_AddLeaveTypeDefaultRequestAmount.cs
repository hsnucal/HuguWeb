using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveTypeDefaultRequestAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultRequestAmount",
                table: "LeaveTypes",
                type: "numeric(6,1)",
                precision: 6,
                scale: 1,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LeaveTypes_DefaultRequestAmount",
                table: "LeaveTypes",
                sql: "\"DefaultRequestAmount\" IS NULL OR (\"DefaultRequestAmount\" > 0 AND (\"DefaultRequestAmount\" * 2) = TRUNC(\"DefaultRequestAmount\" * 2))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LeaveTypes_DefaultRequestAmount",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DefaultRequestAmount",
                table: "LeaveTypes");
        }
    }
}
