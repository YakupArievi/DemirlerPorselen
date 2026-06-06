using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Toptanci.Application.Common.Abstractions;
using Toptanci.Application.Features.Reporting.Reports;
using Toptanci.Application.Features.Sales.Accounts;

namespace Toptanci.Infrastructure.Reporting;

public sealed class ReportPdfService : IReportPdfService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    static ReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string Money(decimal v) => v.ToString("N2", Tr) + " TL";
    private static string Date(DateTime d) => d.ToString("dd.MM.yyyy", Tr);
    private static string DateTimeStr(DateTime d) => d.ToString("dd.MM.yyyy HH:mm", Tr);

    public byte[] AccountStatement(AccountStatementDto s)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                Setup(page, "Cari Ekstre");
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Müşteri: {s.CustomerName}").Bold().FontSize(12);
                    col.Item().Text($"Dönem: {Date(s.FromDate)} - {Date(s.ToDate)}");
                    col.Item().Text($"Açılış Bakiyesi: {Money(s.OpeningBalance)}");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); });
                        table.Header(h =>
                        {
                            HeaderCell(h, "Tarih"); HeaderCell(h, "Tip"); HeaderCell(h, "Borç"); HeaderCell(h, "Alacak"); HeaderCell(h, "Bakiye");
                        });
                        foreach (var l in s.Lines)
                        {
                            Cell(table, Date(l.Date));
                            Cell(table, l.Type.ToString());
                            Cell(table, l.Debit == 0 ? "" : Money(l.Debit));
                            Cell(table, l.Credit == 0 ? "" : Money(l.Credit));
                            Cell(table, Money(l.RunningBalance));
                        }
                    });

                    col.Item().AlignRight().Text($"Toplam Borç: {Money(s.TotalDebit)}   Toplam Alacak: {Money(s.TotalCredit)}");
                    col.Item().AlignRight().Text($"Kapanış Bakiyesi: {Money(s.ClosingBalance)}").Bold().FontSize(12);
                });
            });
        }).GeneratePdf();
    }

    public byte[] CustomerSummary(CustomerSummaryDto s)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                Setup(page, "Müşteri Özeti");
                page.Content().Column(col =>
                {
                    col.Spacing(6);
                    col.Item().Text($"Müşteri: {s.Name}").Bold().FontSize(12);
                    if (!string.IsNullOrWhiteSpace(s.Phone)) col.Item().Text($"Telefon: {s.Phone}");
                    col.Item().Text($"Açılış Bakiyesi: {Money(s.OpeningBalance)}");
                    col.Item().Text($"Toplam Satış: {Money(s.TotalSales)} ({s.SaleCount} fiş)");
                    col.Item().Text($"Toplam Tahsilat: {Money(s.TotalCollections)}");
                    col.Item().PaddingTop(6).Text($"Güncel Bakiye: {Money(s.Balance)}").Bold().FontSize(14);
                });
            });
        }).GeneratePdf();
    }

    public byte[] StockReport(StockReportDto r)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                Setup(page, "Stok Raporu");
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Depo: {r.WarehouseName ?? "Tüm Depolar"}");
                    col.Item().Text($"Toplam Satır: {r.TotalLines}   Kritik: {r.CriticalLines}");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); });
                        table.Header(h =>
                        {
                            HeaderCell(h, "Ürün"); HeaderCell(h, "Renk/Beden"); HeaderCell(h, "Barkod"); HeaderCell(h, "Depo"); HeaderCell(h, "Miktar"); HeaderCell(h, "Min");
                        });
                        foreach (var row in r.Rows)
                        {
                            Cell(table, row.ProductName);
                            Cell(table, $"{row.Color} {row.Size}".Trim());
                            Cell(table, row.AdetBarcode);
                            Cell(table, row.WarehouseName);
                            table.Cell().Element(c => CellStyle(c, row.IsBelowMin)).Text(row.Quantity.ToString());
                            Cell(table, row.MinStock.ToString());
                        }
                    });
                });
            });
        }).GeneratePdf();
    }

    public byte[] Profitability(ProfitabilityReportDto r)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                Setup(page, "Karlılık Raporu");
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text($"Dönem: {Date(r.FromDate)} - {Date(r.ToDate)}");
                    col.Item().Text($"Toplam Satış: {Money(r.TotalRevenue)}   Maliyet: {Money(r.TotalCost)}   Kar: {Money(r.TotalProfit)}").Bold();
                    col.Item().Text($"Marj: %{r.MarginPercent.ToString("N2", Tr)}   Markup: %{r.MarkupPercent.ToString("N2", Tr)}");

                    col.Item().PaddingTop(6).Text("Ürün Bazlı").Bold();
                    ProfitTable(col, r.ByProduct);

                    col.Item().PaddingTop(6).Text("Kategori Bazlı").Bold();
                    ProfitTable(col, r.ByCategory);
                });
            });
        }).GeneratePdf();
    }

    private static void ProfitTable(ColumnDescriptor col, IReadOnlyList<ProfitRowDto> rows)
    {
        col.Item().Table(table =>
        {
            table.ColumnsDefinition(c => { c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); });
            table.Header(h =>
            {
                HeaderCell(h, "Ad"); HeaderCell(h, "Satış"); HeaderCell(h, "Maliyet"); HeaderCell(h, "Kar"); HeaderCell(h, "Marj%"); HeaderCell(h, "Markup%");
            });
            foreach (var row in rows)
            {
                Cell(table, row.Name);
                Cell(table, Money(row.Revenue));
                Cell(table, Money(row.Cost));
                Cell(table, Money(row.Profit));
                Cell(table, row.MarginPercent.ToString("N1", Tr));
                Cell(table, row.MarkupPercent.ToString("N1", Tr));
            }
        });
    }

    private static void Setup(PageDescriptor page, string title)
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.DefaultTextStyle(t => t.FontSize(9));
        page.Header().Column(c =>
        {
            c.Item().Text("Toptancı Stok & Cari Yönetim").FontSize(14).Bold();
            c.Item().Text(title).FontSize(11).FontColor(Colors.Grey.Darken2);
            c.Item().Text($"Oluşturma: {DateTimeStr(DateTime.Now)}").FontSize(8).FontColor(Colors.Grey.Medium);
        });
        page.Footer().AlignCenter().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); });
    }

    private static void HeaderCell(TableCellDescriptor h, string text)
        => h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(text).Bold();

    private static void Cell(TableDescriptor table, string text)
        => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(text);

    private static IContainer CellStyle(IContainer c, bool critical)
    {
        c = c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);
        return critical ? c.Background(Colors.Red.Lighten4) : c;
    }
}
