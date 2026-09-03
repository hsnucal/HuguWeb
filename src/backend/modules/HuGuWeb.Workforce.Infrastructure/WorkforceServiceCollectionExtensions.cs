using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using HuGuWeb.Workforce.Infrastructure.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HuGuWeb.Workforce.Infrastructure;

public static class WorkforceServiceCollectionExtensions
{
    public static IServiceCollection AddWorkforceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.Configure<WorkplaceOptions>(configuration.GetSection(WorkplaceOptions.SectionName));
        services.Configure<EmployeePhotoStorageOptions>(
            configuration.GetSection(EmployeePhotoStorageOptions.SectionName));
        services.AddDbContext<WorkforceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IWorkforceClock, SystemWorkforceClock>();
        services.AddSingleton<IWorkplaceContext, ConfiguredWorkplaceContext>();
        services.AddSingleton<IEmployeePhotoStorage, FileSystemEmployeePhotoStorage>();
        services.AddScoped<IWorkforceStore, EfWorkforceStore>();
        services.AddScoped<HireEmployeeUseCase>();
        services.AddScoped<HireEmployeeWithProfileUseCase>();
        services.AddScoped<UpdateEmployeeHrProfileUseCase>();
        services.AddScoped<EmployeePhotoUseCases>();
        services.AddScoped<TransferEmployeeUseCase>();
        services.AddScoped<EndEmploymentUseCase>();
        services.AddScoped<MaintainDepartmentsUseCase>();
        services.AddScoped<MaintainPositionsUseCase>();
        services.AddScoped<ActiveWorkforceQuery>();
        services.AddScoped<EmployeeHistoryQuery>();
        services.AddScoped<EmployeeDirectoryQuery>();
        services.AddScoped<HrEmployeeDirectoryQuery>();
        services.AddScoped<HrEmployeeCardQuery>();
        services.AddScoped<OfficialLookupsQuery>();
        services.AddScoped<MaintainSgkWorkplaceRegistrationsUseCase>();
        services.AddScoped<SaveOfficialEmploymentProfileUseCase>();
        services.AddSingleton<PersonnelImportPreviewStore>();
        services.AddSingleton<IPersonnelSpreadsheetService, ClosedXmlPersonnelSpreadsheetService>();
        services.AddScoped<PersonnelExcelExportUseCase>();
        services.AddScoped<PersonnelExcelImportUseCase>();
        services.AddScoped<SaveEmployeePaymentProfileUseCase>();
        services.AddScoped<PersonnelProfileHistoryQuery>();
        services.AddScoped<EnsureDefaultLeaveTypesUseCase>();
        services.AddScoped<EnsurePersonnelEnrichmentDefaultsUseCase>();
        services.AddScoped<LeaveTypeAdminUseCase>();
        services.AddScoped<OnboardingCatalogQuery>();
        services.AddScoped<OnboardingChecklistQuery>();
        services.AddScoped<ListOnboardingDocumentRequirementsQuery>();
        services.AddScoped<SetOnboardingChecklistItemUseCase>();
        services.AddScoped<SyncOnboardingChecklistUseCase>();
        services.AddScoped<CompleteEmploymentOnboardingUseCase>();
        services.AddScoped<HrDocumentTemplateQuery>();
        services.AddScoped<PreviewHrDocumentTemplateUseCase>();
        services.AddScoped<PreviewHrDocumentTemplateDraftUseCase>();
        services.AddScoped<RenderHrDocumentDocxUseCase>();
        services.AddScoped<RenderHrDocumentDraftDocxUseCase>();
        services.AddScoped<ListRecruitmentSourcesQuery>();
        services.AddScoped<EmployeeLeaveQuery>();
        services.AddScoped<RecordLeaveEntitlementUseCase>();
        services.AddScoped<RecordLeaveUseCase>();
        services.AddScoped<CancelLeaveRecordUseCase>();
        services.AddScoped<CreateLeaveRequestUseCase>();
        services.AddScoped<ApproveLeaveRequestDepartmentUseCase>();
        services.AddScoped<ApproveLeaveRequestHrUseCase>();
        services.AddScoped<RejectLeaveRequestUseCase>();
        services.AddScoped<WithdrawLeaveRequestUseCase>();
        services.AddScoped<CancelApprovedLeaveRequestUseCase>();
        services.AddScoped<LeaveRequestComposer>();
        services.AddScoped<LeaveRequestQuery>();
        services.AddScoped<CreateMyLeaveRequestUseCase>();
        services.AddScoped<PreviewLeaveRequestUseCase>();
        services.AddScoped<LeaveRequestActionUseCase>();
        services.AddScoped<MyLeaveSelfServiceQuery>();
        services.AddScoped<ShiftDefinitionAdminUseCase>();
        services.AddScoped<UpsertScheduleEntryUseCase>();
        services.AddScoped<ClearScheduleEntryUseCase>();
        services.AddScoped<GetScheduleStateQuery>();
        services.AddScoped<GetScheduleRangeQuery>();
        services.AddScoped<GetScheduleWeekQuery>();
        services.AddScoped<BulkScheduleUseCase>();
        services.AddScoped<CopyScheduleWeekUseCase>();
        services.AddScoped<GetAttendanceMonthQuery>();
        services.AddScoped<SetAttendanceCorrectionUseCase>();
        services.AddScoped<ClearAttendanceCorrectionUseCase>();
        services.AddScoped<GetAttendanceCorrectionHistoryQuery>();
        return services;
    }
}
