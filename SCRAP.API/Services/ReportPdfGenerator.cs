using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCRAP.domain.entities;

namespace SCRAP.API.Services
{
    public static class ReportPdfGenerator
    {
        public static byte[] GenerateInventoryReport(List<Inventory> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Inventory History Report").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Total records: {items.Count}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Device Name", "Category", "Serial Number", "Status", "Date Received", "Storage", "Notes" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in items)
                        {
                            table.Cell().Element(CellStyle).Text(item.Id.ToString());
                            table.Cell().Element(CellStyle).Text(item.DeviceName);
                            table.Cell().Element(CellStyle).Text(item.DeviceCategory?.Name ?? "");
                            table.Cell().Element(CellStyle).Text(item.SerialNumber);
                            table.Cell().Element(CellStyle).Text(item.Status.ToString());
                            table.Cell().Element(CellStyle).Text(item.DateReceived.ToString("MMM dd, yyyy"));
                            table.Cell().Element(CellStyle).Text(item.HasStorageDevice ? "Yes" : "No");
                            table.Cell().Element(CellStyle).Text(item.Notes ?? "");
                        }

                        static IContainer CellStyle(IContainer c) =>
                            c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                    });

                    page.Footer().AlignCenter().Text(x =>
                        x.Span("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1));
                });
            }).GeneratePdf();
        }

        public static byte[] GenerateTeardownReport(List<TeardownBatch> batches)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Teardown History Report").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Total batches: {batches.Count}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(14);

                        foreach (var batch in batches)
                        {
                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(inner =>
                            {
                                inner.Item().Text($"Batch #{batch.Id} — {batch.DeviceCategory?.Name ?? "Unknown Category"}").Bold().FontSize(12);
                                inner.Item().Text($"Quantity Dismantled: {batch.QuantityDismantled}   |   Date: {batch.DateProcessed:MMM dd, yyyy h:mm tt}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                                if (batch.Yields != null && batch.Yields.Count > 0)
                                {
                                    inner.Item().PaddingTop(6).Table(table =>
                                    {
                                        table.ColumnsDefinition(c =>
                                        {
                                            c.RelativeColumn(3);
                                            c.RelativeColumn(2);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(HeaderStyle).Text("Material");
                                            header.Cell().Element(HeaderStyle).Text("Weight (kg)");

                                            static IContainer HeaderStyle(IContainer c) =>
                                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                                        });

                                        foreach (var y in batch.Yields)
                                        {
                                            table.Cell().Element(CellStyle).Text(y.MaterialName);
                                            table.Cell().Element(CellStyle).Text(y.WeightKg.ToString("N4"));

                                            static IContainer CellStyle(IContainer c) =>
                                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                                        }
                                    });
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                        x.Span("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1));
                });
            }).GeneratePdf();
        }

        public static byte[] GenerateSalesReport(List<CommoditySale> sales)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var totalRevenue = sales.Sum(s => s.TotalAmount);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Sales History Report").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Total sales: {sales.Count}   |   Total revenue: {totalRevenue:N2}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Material", "Buyer", "Qty (kg)", "Price/kg", "Total", "Invoice #", "Date" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var s in sales)
                        {
                            table.Cell().Element(CellStyle).Text(s.Id.ToString());
                            table.Cell().Element(CellStyle).Text(s.MaterialName);
                            table.Cell().Element(CellStyle).Text(s.BuyerName);
                            table.Cell().Element(CellStyle).Text(s.QuantityKg.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.PricePerKg.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.TotalAmount.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.InvoiceNumber);
                            table.Cell().Element(CellStyle).Text(s.SaleDate.ToString("MMM dd, yyyy"));
                        }

                        static IContainer CellStyle(IContainer c) =>
                            c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                    });

                    page.Footer().AlignCenter().Text(x =>
                        x.Span("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1));
                });
            }).GeneratePdf();
        }
        public static byte[] GenerateOverallReport(List<Inventory> inventory, List<TeardownBatch> teardowns, List<CommoditySale> sales)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var totalRevenue = sales.Sum(s => s.TotalAmount);

            return Document.Create(container =>
            {
                // --- Cover / summary page ---
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("S.C.R.A.P — Overall Report").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}").FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(20).Text("Summary").FontSize(16).Bold();
                        col.Item().PaddingTop(8).Text($"Total Inventory Records: {inventory.Count}");
                        col.Item().Text($"Total Teardown Batches: {teardowns.Count}");
                        col.Item().Text($"Total Sales Transactions: {sales.Count}");
                        col.Item().Text($"Total Sales Revenue: {totalRevenue:N2}");
                    });
                });

                // --- Inventory section ---
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text("Inventory History").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Device Name", "Category", "Serial Number", "Status", "Date Received", "Storage" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in inventory)
                        {
                            table.Cell().Element(CellStyle).Text(item.Id.ToString());
                            table.Cell().Element(CellStyle).Text(item.DeviceName);
                            table.Cell().Element(CellStyle).Text(item.DeviceCategory?.Name ?? "");
                            table.Cell().Element(CellStyle).Text(item.SerialNumber);
                            table.Cell().Element(CellStyle).Text(item.Status.ToString());
                            table.Cell().Element(CellStyle).Text(item.DateReceived.ToString("MMM dd, yyyy"));
                            table.Cell().Element(CellStyle).Text(item.HasStorageDevice ? "Yes" : "No");

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                // --- Teardown section ---
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text("Teardown History").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Spacing(12);
                        foreach (var batch in teardowns)
                        {
                            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(inner =>
                            {
                                inner.Item().Text($"Batch #{batch.Id} — {batch.DeviceCategory?.Name ?? "Unknown"}").Bold();
                                inner.Item().Text($"Qty: {batch.QuantityDismantled}   Date: {batch.DateProcessed:MMM dd, yyyy}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken1);

                                if (batch.Yields != null)
                                {
                                    foreach (var y in batch.Yields)
                                        inner.Item().Text($"  • {y.MaterialName}: {y.WeightKg:N4} kg").FontSize(9);
                                }
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                // --- Sales section ---
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text("Sales History").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Material", "Buyer", "Qty (kg)", "Price/kg", "Total", "Invoice #", "Date" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var s in sales)
                        {
                            table.Cell().Element(CellStyle).Text(s.Id.ToString());
                            table.Cell().Element(CellStyle).Text(s.MaterialName);
                            table.Cell().Element(CellStyle).Text(s.BuyerName);
                            table.Cell().Element(CellStyle).Text(s.QuantityKg.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.PricePerKg.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.TotalAmount.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(s.InvoiceNumber);
                            table.Cell().Element(CellStyle).Text(s.SaleDate.ToString("MMM dd, yyyy"));

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }
    }
}