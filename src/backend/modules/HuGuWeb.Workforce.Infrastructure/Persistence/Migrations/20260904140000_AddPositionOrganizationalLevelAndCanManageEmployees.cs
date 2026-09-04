using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(WorkforceDbContext))]
    [Migration("20260904140000_AddPositionOrganizationalLevelAndCanManageEmployees")]
    public class AddPositionOrganizationalLevelAndCanManageEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanManageEmployees",
                table: "Positions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationalLevel",
                table: "Positions",
                type: "integer",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanManageEmployees",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "OrganizationalLevel",
                table: "Positions");
        }
    }
}
