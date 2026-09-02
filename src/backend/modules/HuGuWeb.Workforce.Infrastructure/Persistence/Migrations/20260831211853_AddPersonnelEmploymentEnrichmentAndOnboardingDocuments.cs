using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonnelEmploymentEnrichmentAndOnboardingDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArgeProjectCode",
                table: "EmployeeHrProfiles");

            migrationBuilder.AddColumn<int>(
                name: "ProbationPeriodMonths",
                table: "Employments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProbationStartDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecruitmentSourceId",
                table: "Employments",
                type: "uuid",
                nullable: true);

            // Existing Employment rows are treated as already finalized for checklist mutation.
            migrationBuilder.AddColumn<string>(
                name: "OnboardingStatus",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Completed");

            // Existing Employment rows receive FullTime as the safe compatibility default.
            // WorkType is independent of ContractType (do not infer from contract).
            migrationBuilder.AddColumn<string>(
                name: "WorkType",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "FullTime");

            migrationBuilder.CreateTable(
                name: "EmployeeCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCertificates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrDocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TemplateAssetPath = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrDocumentTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrDocumentTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingDocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequiredByDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingDocumentRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingDocumentRequirements_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecruitmentSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentSources_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentOnboardingDocumentStatuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequirementId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentOnboardingDocumentStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentOnboardingDocumentStatuses_Employments_Employment~",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmploymentOnboardingDocumentStatuses_OnboardingDocumentRequ~",
                        column: x => x.RequirementId,
                        principalTable: "OnboardingDocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employments_RecruitmentSourceId",
                table: "Employments",
                column: "RecruitmentSourceId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employments_Probation",
                table: "Employments",
                sql: "(\"ProbationPeriodMonths\" IS NULL AND \"ProbationStartDate\" IS NULL) OR (\"ProbationPeriodMonths\" = 2 AND \"ProbationStartDate\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertificates_EmployeeId",
                table: "EmployeeCertificates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentOnboardingDocumentStatuses_EmploymentId_Requireme~",
                table: "EmploymentOnboardingDocumentStatuses",
                columns: new[] { "EmploymentId", "RequirementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentOnboardingDocumentStatuses_RequirementId",
                table: "EmploymentOnboardingDocumentStatuses",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDocumentTemplates_OrganizationId_Category_IsActive",
                table: "HrDocumentTemplates",
                columns: new[] { "OrganizationId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_HrDocumentTemplates_OrganizationId_Code",
                table: "HrDocumentTemplates",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDocumentRequirements_OrganizationId_Code",
                table: "OnboardingDocumentRequirements",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingDocumentRequirements_OrganizationId_IsActive",
                table: "OnboardingDocumentRequirements",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentSources_OrganizationId_Code",
                table: "RecruitmentSources",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentSources_OrganizationId_IsActive",
                table: "RecruitmentSources",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employments_RecruitmentSources_RecruitmentSourceId",
                table: "Employments",
                column: "RecruitmentSourceId",
                principalTable: "RecruitmentSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employments_RecruitmentSources_RecruitmentSourceId",
                table: "Employments");

            migrationBuilder.DropTable(
                name: "EmployeeCertificates");

            migrationBuilder.DropTable(
                name: "EmploymentOnboardingDocumentStatuses");

            migrationBuilder.DropTable(
                name: "HrDocumentTemplates");

            migrationBuilder.DropTable(
                name: "RecruitmentSources");

            migrationBuilder.DropTable(
                name: "OnboardingDocumentRequirements");

            migrationBuilder.DropIndex(
                name: "IX_Employments_RecruitmentSourceId",
                table: "Employments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employments_Probation",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "ProbationPeriodMonths",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "ProbationStartDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "RecruitmentSourceId",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "WorkType",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "OnboardingStatus",
                table: "Employments");

            migrationBuilder.AddColumn<string>(
                name: "ArgeProjectCode",
                table: "EmployeeHrProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
