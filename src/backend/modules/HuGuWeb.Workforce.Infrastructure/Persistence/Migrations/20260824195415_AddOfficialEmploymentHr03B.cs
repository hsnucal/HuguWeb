using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HuGuWeb.Workforce.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficialEmploymentHr03B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEndDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "IncentiveEndDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "IncentiveStartDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IskurStatus",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IskurWorkforceStatus",
                table: "Employments",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartTimeMonthlyHours",
                table: "Employments",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkPermitEndDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkPermitStartDate",
                table: "Employments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArgeProjectCode",
                table: "EmployeeHrProfiles",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DrivingLicenceCategory",
                table: "EmployeeHrProfiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EducationDescription",
                table: "EmployeeHrProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ForeignLanguage",
                table: "EmployeeHrProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "GraduationDate",
                table: "EmployeeHrProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KepAddress",
                table: "EmployeeHrProfiles",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MilitaryDefermentReason",
                table: "EmployeeHrProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MilitaryExemptionReason",
                table: "EmployeeHrProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MilitaryServiceStatus",
                table: "EmployeeHrProfiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SchoolName",
                table: "EmployeeHrProfiles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicableLawCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicableLawCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentBesSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeductionEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    ExtraAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentBesSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentBesSettings_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentDutyCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentDutyCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceBranches",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceBranches", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SgkDocumentTypes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SgkDocumentTypes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SgkOccupationCodes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CatalogueVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SgkOccupationCodes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SgkWorkplaceRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SgkWorkplaceRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SgkWorkplaceRegistrations_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OfficialEmploymentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SgkWorkplaceRegistrationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentTypeCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    ApplicableLawCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    InsuranceBranchCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    OccupationCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    DutyCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialEmploymentProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_ApplicableLawCodes_ApplicableLaw~",
                        column: x => x.ApplicableLawCode,
                        principalTable: "ApplicableLawCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_EmploymentDutyCodes_DutyCode",
                        column: x => x.DutyCode,
                        principalTable: "EmploymentDutyCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_Employments_EmploymentId",
                        column: x => x.EmploymentId,
                        principalTable: "Employments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_InsuranceBranches_InsuranceBranc~",
                        column: x => x.InsuranceBranchCode,
                        principalTable: "InsuranceBranches",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_SgkDocumentTypes_DocumentTypeCode",
                        column: x => x.DocumentTypeCode,
                        principalTable: "SgkDocumentTypes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_SgkOccupationCodes_OccupationCode",
                        column: x => x.OccupationCode,
                        principalTable: "SgkOccupationCodes",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficialEmploymentProfiles_SgkWorkplaceRegistrations_SgkWor~",
                        column: x => x.SgkWorkplaceRegistrationId,
                        principalTable: "SgkWorkplaceRegistrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employments_IncentiveRange",
                table: "Employments",
                sql: "\"IncentiveEndDate\" IS NULL OR \"IncentiveStartDate\" IS NULL OR \"IncentiveEndDate\" >= \"IncentiveStartDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Employments_WorkPermitRange",
                table: "Employments",
                sql: "\"WorkPermitEndDate\" IS NULL OR \"WorkPermitStartDate\" IS NULL OR \"WorkPermitEndDate\" >= \"WorkPermitStartDate\"");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentBesSettings_EmploymentId",
                table: "EmploymentBesSettings",
                column: "EmploymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_ApplicableLawCode",
                table: "OfficialEmploymentProfiles",
                column: "ApplicableLawCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_DocumentTypeCode",
                table: "OfficialEmploymentProfiles",
                column: "DocumentTypeCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_DutyCode",
                table: "OfficialEmploymentProfiles",
                column: "DutyCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_EmploymentId",
                table: "OfficialEmploymentProfiles",
                column: "EmploymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_InsuranceBranchCode",
                table: "OfficialEmploymentProfiles",
                column: "InsuranceBranchCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_OccupationCode",
                table: "OfficialEmploymentProfiles",
                column: "OccupationCode");

            migrationBuilder.CreateIndex(
                name: "IX_OfficialEmploymentProfiles_SgkWorkplaceRegistrationId",
                table: "OfficialEmploymentProfiles",
                column: "SgkWorkplaceRegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_SgkOccupationCodes_Description",
                table: "SgkOccupationCodes",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_SgkOccupationCodes_IsActive",
                table: "SgkOccupationCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SgkWorkplaceRegistrations_PropertyId",
                table: "SgkWorkplaceRegistrations",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_SgkWorkplaceRegistrations_PropertyId_IsActive",
                table: "SgkWorkplaceRegistrations",
                columns: new[] { "PropertyId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmploymentBesSettings");

            migrationBuilder.DropTable(
                name: "OfficialEmploymentProfiles");

            migrationBuilder.DropTable(
                name: "ApplicableLawCodes");

            migrationBuilder.DropTable(
                name: "EmploymentDutyCodes");

            migrationBuilder.DropTable(
                name: "InsuranceBranches");

            migrationBuilder.DropTable(
                name: "SgkDocumentTypes");

            migrationBuilder.DropTable(
                name: "SgkOccupationCodes");

            migrationBuilder.DropTable(
                name: "SgkWorkplaceRegistrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employments_IncentiveRange",
                table: "Employments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Employments_WorkPermitRange",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "ContractEndDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "IncentiveEndDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "IncentiveStartDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "IskurStatus",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "IskurWorkforceStatus",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "PartTimeMonthlyHours",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "WorkPermitEndDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "WorkPermitStartDate",
                table: "Employments");

            migrationBuilder.DropColumn(
                name: "ArgeProjectCode",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "DrivingLicenceCategory",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "EducationDescription",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "ForeignLanguage",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "GraduationDate",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "KepAddress",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "MilitaryDefermentReason",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "MilitaryExemptionReason",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "MilitaryServiceStatus",
                table: "EmployeeHrProfiles");

            migrationBuilder.DropColumn(
                name: "SchoolName",
                table: "EmployeeHrProfiles");
        }
    }
}
