using System;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using ExpenseHub.Api.Contracts;
using ExpenseHub.Core.Reports;

namespace ExpenseHub.Api.Application;

internal static class ReportExcelExporter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string AmountFormat = "#,##0.00";
    private const int HeaderRow = 7;

    public static byte[] Export(ReportResponse report)
    {
        ArgumentNullException.ThrowIfNull(report);
        ReportResult result = report.Result
            ?? throw new ArgumentException("O relatório precisa estar concluído e ter resultado salvo.", nameof(report));

        using XLWorkbook workbook = new();
        IXLWorksheet sheet = workbook.Worksheets.Add("Relatório");

        sheet.Cell(1, 1).Value = "Identificador";
        sheet.Cell(1, 2).Value = report.Id.ToString();
        sheet.Cell(2, 1).Value = "Escopo";
        sheet.Cell(2, 2).Value = report.Scope.ToString();
        sheet.Cell(3, 1).Value = "Data inicial";
        sheet.Cell(3, 2).Value = report.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(4, 1).Value = "Data final";
        sheet.Cell(4, 2).Value = report.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        sheet.Cell(5, 1).Value = "Gerado em (UTC)";
        sheet.Cell(5, 2).Value = DateTime.SpecifyKind(result.GeneratedAt, DateTimeKind.Utc)
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        sheet.Cell(HeaderRow, 1).Value = "Estado";
        sheet.Cell(HeaderRow, 2).Value = "Quantidade";
        sheet.Cell(HeaderRow, 3).Value = "Valor";
        sheet.Row(HeaderRow).Style.Font.Bold = true;

        int row = HeaderRow + 1;
        foreach (ReportStatusTotal total in result.ByStatus)
        {
            WriteLine(sheet, row, total.Status.ToString(), total.Count, total.Amount);
            row++;
        }

        WriteLine(sheet, row, "Total", result.TotalCount, result.TotalAmount);
        sheet.Row(row).Style.Font.Bold = true;
        sheet.Columns(1, 3).AdjustToContents();

        using MemoryStream stream = new();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteLine(IXLWorksheet sheet, int row, string label, int count, decimal amount)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 2).Value = count;
        sheet.Cell(row, 3).Value = amount;
        sheet.Cell(row, 3).Style.NumberFormat.Format = AmountFormat;
    }
}
