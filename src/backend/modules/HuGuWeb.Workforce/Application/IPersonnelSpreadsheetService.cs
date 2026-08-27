namespace HuGuWeb.Workforce.Application;

public interface IPersonnelSpreadsheetService
{
    byte[] BuildImportTemplate(PersonnelImportTemplateContext context);

    byte[] BuildExportWorkbook(
        IReadOnlyList<PersonnelExportColumn> columns,
        IReadOnlyList<PersonnelExportRow> rows);

    PersonnelSpreadsheetParseResult ParseImportWorkbook(Stream content, long length);
}

public sealed record PersonnelExportColumn(string Id, string Header);

public sealed record PersonnelExportRow(IReadOnlyDictionary<string, string?> Values);

public sealed record PersonnelSpreadsheetParseResult(
    bool IsSuccess,
    IReadOnlyList<PersonnelImportRawRow> Rows,
    string? ErrorCode);

public sealed record PersonnelImportRawRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Cells);
