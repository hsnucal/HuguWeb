using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmploymentWorkingConditionsHr04 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "SeniorityStartDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationReason",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employments_SeniorityStartDate",
                table: "Employments",
                sql: "\"SeniorityStartDate\" IS NULL OR \"SeniorityStartDate\" <= \"StartDate\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Employments_SeniorityStartDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "SeniorityStartDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                table: "Employments");
        }
    }
}
