using ClosedXML.Excel;
using TicketFlow.Shared.DTOs;

namespace TicketFlow.API.Reporting;

public static class TicketExcelExporter
{
    public static byte[] Export(IEnumerable<TicketDto> tickets)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Tickets");

        string[] headers =
        [
            "Ticket #", "Title", "Status", "Priority", "Category", "Project",
            "Customer", "Agent", "Comments", "Created At", "Due Date", "Resolved At"
        ];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");

        var row = 2;
        foreach (var t in tickets)
        {
            sheet.Cell(row, 1).Value = t.TicketNumber;
            sheet.Cell(row, 2).Value = t.Title;
            sheet.Cell(row, 3).Value = t.Status;
            sheet.Cell(row, 4).Value = t.Priority;
            sheet.Cell(row, 5).Value = t.CategoryName;
            sheet.Cell(row, 6).Value = t.ProjectName ?? "";
            sheet.Cell(row, 7).Value = t.UserName;
            sheet.Cell(row, 8).Value = t.AgentName ?? "Unassigned";
            sheet.Cell(row, 9).Value = t.CommentCount;
            sheet.Cell(row, 10).Value = t.CreatedAt;
            sheet.Cell(row, 10).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            if (t.DueDate.HasValue)
            {
                sheet.Cell(row, 11).Value = t.DueDate.Value;
                sheet.Cell(row, 11).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }
            if (t.ResolvedAt.HasValue)
            {
                sheet.Cell(row, 12).Value = t.ResolvedAt.Value;
                sheet.Cell(row, 12).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
            }
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
