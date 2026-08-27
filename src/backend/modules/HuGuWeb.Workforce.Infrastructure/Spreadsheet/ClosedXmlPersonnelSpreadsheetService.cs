using System.Globalization;
using ClosedXML.Excel;
using HuGuWeb.Workforce.Application;

namespace HuGuWeb.Workforce.Infrastructure.Spreadsheet;

public sealed class ClosedXmlPersonnelSpreadsheetService : IPersonnelSpreadsheetService
{
    private const string SheetName = "Personnel";
    private const string HelpSheetName = "Yardım";
    private const string ReferenceSheetName = "Referans";
    private const string ListsSheetName = "_lists";
    private const string MetaSheetName = "_meta";
    private const string BrandFill = PersonnelImportColumnCatalog.BrandFill;
    private const double MinColumnWidth = PersonnelImportColumnCatalog.MinColumnWidth;
    private const double MaxColumnWidth = 50d;
    private const double TemplateMaxColumnWidth = PersonnelImportColumnCatalog.MaxColumnWidth;

    public byte[] BuildImportTemplate(PersonnelImportTemplateContext context)
    {
        using var workbook = new XLWorkbook();
        var columns = PersonnelImportColumnCatalog.Columns;
        var meta = workbook.Worksheets.Add(MetaSheetName);
        meta.Visibility = XLWorksheetVisibility.VeryHidden;
        meta.Cell(1, 1).Value = PersonnelImportColumnCatalog.WorkbookVersion;

        var lists = workbook.Worksheets.Add(ListsSheetName);
        lists.Visibility = XLWorksheetVisibility.Hidden;

        var sheet = workbook.Worksheets.Add(SheetName);
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            var cell = sheet.Cell(1, index + 1);
            cell.Value = PersonnelImportColumnCatalog.DisplayHeader(column);
            meta.Cell(2, index + 1).Value = column.Id;
            if (column.WrapText)
            {
                sheet.Column(index + 1).Style.Alignment.WrapText = true;
            }
        }

        WriteEnumLists(lists, columns);
        ApplyHeaderPresentation(sheet, columns.Count, dataRowCount: 0, TemplateMaxColumnWidth);
        sheet.Row(1).Style.Alignment.WrapText = true;
        sheet.Row(1).Height = 32;
        ApplyTemplateValidations(sheet, lists, columns, context);
        ApplyTextFormats(sheet, columns);
        WriteHelpSheet(workbook);
        WriteReferenceSheet(workbook, context);

        sheet.Position = 1;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

  public byte[] BuildExportWorkbook(
        IReadOnlyList<PersonnelExportColumn> columns,
        IReadOnlyList<PersonnelExportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);
        var columnCount = columns.Count;

        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            sheet.Cell(1, columnIndex + 1).Value = columns[columnIndex].Header;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var column = columns[columnIndex];
                row.Values.TryGetValue(column.Id, out var value);
                WriteExportCell(sheet.Cell(rowIndex + 2, columnIndex + 1), column.Id, value);
            }
        }

        ApplyExportPresentation(sheet, columnCount, rows.Count);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ApplyHeaderPresentation(IXLWorksheet sheet, int columnCount, int dataRowCount, double maxWidth)
    {
        if (columnCount == 0)
        {
            return;
        }

        var lastRow = Math.Max(1, dataRowCount + 1);
        var used = sheet.Range(1, 1, lastRow, columnCount);
        used.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        used.Style.Border.InsideBorderColor = XLColor.FromHtml("#E8D7DE");
        used.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E8D7DE");

        var headerRow = sheet.Row(1);
        headerRow.Height = 22;
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandFill);
        headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        sheet.SheetView.FreezeRows(1);
        used.SetAutoFilter();

        for (var columnIndex = 1; columnIndex <= columnCount; columnIndex++)
        {
            FitColumnWidth(sheet.Column(columnIndex), lastRow, maxWidth);
        }
    }

    private static void ApplyExportPresentation(IXLWorksheet sheet, int columnCount, int dataRowCount) =>
        ApplyHeaderPresentation(sheet, columnCount, dataRowCount, MaxColumnWidth);

    private static void FitColumnWidth(IXLColumn column, int lastRow, double maxWidth)
    {
        column.AdjustToContents(1, lastRow);
        var fitted = Math.Max(column.Width, EstimateContentWidth(column, lastRow));
        column.Width = Math.Clamp(fitted, MinColumnWidth, maxWidth);
    }

    private static double EstimateContentWidth(IXLColumn column, int lastRow)
    {
        var maxLength = 0;
        for (var row = 1; row <= lastRow; row++)
        {
            maxLength = Math.Max(maxLength, column.Cell(row).GetFormattedString().Length);
        }

        return maxLength * 1.2d + 2.5d;
    }

    private static void WriteExportCell(IXLCell cell, string columnId, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            cell.Clear();
            return;
        }

        if (IsTextColumn(columnId))
        {
            var safe = SpreadsheetSafety.SanitizeCellValue(value);
            cell.SetValue(safe);
            cell.Style.NumberFormat.Format = "@";
            return;
        }

        if (string.Equals(columnId, "startDate", StringComparison.OrdinalIgnoreCase)
            && DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            cell.SetValue(date.ToDateTime(TimeOnly.MinValue));
            cell.Style.DateFormat.Format = "yyyy-mm-dd";
            return;
        }

        var sanitized = SpreadsheetSafety.SanitizeCellValue(value);
        cell.SetValue(sanitized);
    }

    private static bool IsTextColumn(string columnId) =>
        columnId.Equals("personnelNumber", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("mobilePhone", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("email", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("nationalIdentityNumber", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("homePhone", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("emergencyPhone", StringComparison.OrdinalIgnoreCase)
        || columnId.Equals("kepAddress", StringComparison.OrdinalIgnoreCase);

    public PersonnelSpreadsheetParseResult ParseImportWorkbook(Stream content, long length)
    {
        try
        {
            using var workbook = new XLWorkbook(content);
            if (workbook.Worksheets.Count == 0)
            {
                return new(false, [], "personnel-import-invalid-file");
            }

            var sheet = workbook.Worksheets.FirstOrDefault(item =>
                string.Equals(item.Name, SheetName, StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheet(1);
            var columnMap = MapColumns(workbook, sheet);
            if (columnMap.Count == 0)
            {
                return new(false, [], "personnel-import-invalid-file");
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var rows = new List<PersonnelImportRawRow>();
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var hasContent = false;
                foreach (var (columnId, columnIndex) in columnMap)
                {
                    var text = sheet.Cell(rowNumber, columnIndex).GetFormattedString().Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        hasContent = true;
                    }

                    cells[columnId] = text;
                }

                if (hasContent)
                {
                    rows.Add(new PersonnelImportRawRow(rowNumber, cells));
                }
            }

            return new(true, rows, null);
        }
        catch
        {
            return new(false, [], "personnel-import-invalid-file");
        }
    }

    private static Dictionary<string, int> MapColumns(XLWorkbook workbook, IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var meta = workbook.Worksheets.FirstOrDefault(item =>
            string.Equals(item.Name, MetaSheetName, StringComparison.OrdinalIgnoreCase));
        if (meta is not null)
        {
            var metaLast = meta.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var column = 1; column <= metaLast; column++)
            {
                var id = meta.Cell(2, column).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(id) && PersonnelImportColumnCatalog.ById.ContainsKey(id))
                {
                    map[id] = column;
                }
            }

            if (map.Count > 0)
            {
                return map;
            }
        }

        var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var column = 1; column <= lastColumn; column++)
        {
            var header = sheet.Cell(1, column).GetString().Trim();
            if (PersonnelImportColumnCatalog.TryMatchHeader(header, out var columnId))
            {
                map[columnId] = column;
            }
        }

        return map;
    }

    private static void WriteEnumLists(IXLWorksheet lists, IReadOnlyList<PersonnelImportColumn> columns)
    {
        var listColumn = 1;
        foreach (var column in columns)
        {
            if (column.ListValues is null || column.ListValues.Count == 0)
            {
                continue;
            }

            lists.Cell(1, listColumn).Value = column.Id;
            for (var index = 0; index < column.ListValues.Count; index++)
            {
                lists.Cell(index + 2, listColumn).Value = column.ListValues[index];
            }

            listColumn++;
        }
    }

    private static void ApplyTemplateValidations(
        IXLWorksheet sheet,
        IXLWorksheet lists,
        IReadOnlyList<PersonnelImportColumn> columns,
        PersonnelImportTemplateContext context)
    {
        var lastDataRow = PersonnelImportLimits.MaxRows + 1;
        var listColumn = 1;
        for (var index = 0; index < columns.Count; index++)
        {
            var column = columns[index];
            if (column.ListValues is null || column.ListValues.Count == 0)
            {
                continue;
            }

            var source = lists.Range(2, listColumn, 1 + column.ListValues.Count, listColumn);
            sheet.Range(2, index + 1, lastDataRow, index + 1).CreateDataValidation().List(source, true);
            listColumn++;
        }

        var departmentCodes = context.Departments
            .Select(item => item.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ApplyCodeList(sheet, lists, columns, PersonnelImportColumnCatalog.Ids.DepartmentCode, departmentCodes, lastDataRow, 20);
        var positionCodes = context.Positions
            .Select(item => item.Code)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ApplyCodeList(sheet, lists, columns, PersonnelImportColumnCatalog.Ids.PositionCode, positionCodes, lastDataRow, 21);
    }

    private static void ApplyCodeList(
        IXLWorksheet sheet,
        IXLWorksheet lists,
        IReadOnlyList<PersonnelImportColumn> columns,
        string columnId,
        string?[] codes,
        int lastDataRow,
        int listColumn)
    {
        if (codes.Length == 0)
        {
            return;
        }

        var columnIndex = columns.ToList().FindIndex(item => item.Id == columnId);
        if (columnIndex < 0)
        {
            return;
        }

        lists.Cell(1, listColumn).Value = columnId + "Values";
        for (var index = 0; index < codes.Length; index++)
        {
            lists.Cell(index + 2, listColumn).Value = codes[index];
        }

        var source = lists.Range(2, listColumn, 1 + codes.Length, listColumn);
        sheet.Range(2, columnIndex + 1, lastDataRow, columnIndex + 1).CreateDataValidation().List(source, true);
    }

    private static void ApplyTextFormats(IXLWorksheet sheet, IReadOnlyList<PersonnelImportColumn> columns)
    {
        var lastDataRow = PersonnelImportLimits.MaxRows + 1;
        for (var index = 0; index < columns.Count; index++)
        {
            if (IsTextColumn(columns[index].Id))
            {
                sheet.Range(2, index + 1, lastDataRow, index + 1).Style.NumberFormat.Format = "@";
            }
        }
    }

    private static void WriteHelpSheet(XLWorkbook workbook)
    {
        var help = workbook.Worksheets.Add(HelpSheetName);
        help.Cell(1, 1).Value = "HuGu personel içe aktarma";
        help.Range(1, 1, 1, 2).Merge();
        ApplyHelpHeader(help);

        var rows = new (string Title, string Body)[]
        {
            ("* = zorunlu alan", "Ad, Soyad, Departman Kodu, Pozisyon Kodu ve İşe Giriş Tarihi yeni kayıt için zorunludur."),
            ("Boş Sicil No", "Yeni personel oluşturur. Sicil numarası sistem tarafından üretilir."),
            ("Dolu Sicil No", "Aynı sicile sahip mevcut personeli günceller. Sicil numarası değiştirilemez."),
            ("Boş güncelleme hücresi", "Mevcut değer korunur; silme anlamına gelmez."),
            ("Tarih biçimi", "yyyy-aa-gg (ör. 2026-01-15) veya Excel tarih hücresi."),
            ("Telefon", "En az 7 rakam. + ile uluslararası biçim kabul edilir."),
            ("Kimlik türü", "Tckn, Ykn, Passport, Other. Kimlik numarası türe uygun olmalıdır."),
            ("Uyruk", "ISO 3166-1 alpha-2 kodu (ör. TR). Ülke adı yazmayın."),
            ("Departman / Pozisyon", "Önce kod ile eşlenir. Kod boşsa mülkte benzersiz ada bakılır. Uygunluk kuralları sunucuda doğrulanır."),
            ("Hassas alanlar", "Kimlik, adres, acil durum ve IBAN için hassas personel okuma yetkisi gerekir. Yetki yoksa bu hücreler doldurulmamalıdır."),
            ("IBAN / Banka", "Maaş veya bordro alanı yoktur. Yalnızca IBAN ve isteğe bağlı banka adı."),
            ("Askerlik", "Muaf ise muaf nedeni, tecilli ise tecil nedeni zorunludur."),
            ("Acil durum", "Bu şablon birincil acil durum kişisini günceller. Ad ve telefon birlikte doldurulmalıdır."),
            ("Satır limiti", "En fazla 5000 satır, dosya en fazla 5 MB."),
        };

        for (var index = 0; index < rows.Length; index++)
        {
            help.Cell(index + 3, 1).Value = rows[index].Title;
            help.Cell(index + 3, 1).Style.Font.Bold = true;
            help.Cell(index + 3, 2).Value = rows[index].Body;
            help.Cell(index + 3, 2).Style.Alignment.WrapText = true;
        }

        help.Column(1).Width = 28;
        help.Column(2).Width = 48;
        help.SheetView.FreezeRows(1);
    }

    private static void ApplyHelpHeader(IXLWorksheet help)
    {
        var header = help.Range(1, 1, 1, 2);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandFill);
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        help.Row(1).Height = 24;
    }

    private static void WriteReferenceSheet(XLWorkbook workbook, PersonnelImportTemplateContext context)
    {
        var sheet = workbook.Worksheets.Add(ReferenceSheetName);
        sheet.Cell(1, 1).Value = "Departman Kodu";
        sheet.Cell(1, 2).Value = "Departman Adı";
        sheet.Cell(1, 4).Value = "Pozisyon Kodu";
        sheet.Cell(1, 5).Value = "Pozisyon Adı";
        var header = sheet.Range(1, 1, 1, 5);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandFill);

        for (var index = 0; index < context.Departments.Count; index++)
        {
            sheet.Cell(index + 2, 1).Value = context.Departments[index].Code ?? string.Empty;
            sheet.Cell(index + 2, 2).Value = context.Departments[index].Name;
        }

        for (var index = 0; index < context.Positions.Count; index++)
        {
            sheet.Cell(index + 2, 4).Value = context.Positions[index].Code ?? string.Empty;
            sheet.Cell(index + 2, 5).Value = context.Positions[index].Name;
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
        foreach (var column in sheet.ColumnsUsed())
        {
            column.Width = Math.Clamp(column.Width, MinColumnWidth, TemplateMaxColumnWidth);
        }
    }
}
