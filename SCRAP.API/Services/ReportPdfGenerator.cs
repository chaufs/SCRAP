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
                        col.Item().Text($"Total sales: {sales.Count}   |   Total revenue: ₱{totalRevenue:N2}").FontSize(9).FontColor(Colors.Grey.Darken1);
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
                            foreach (var h in new[] { "ID", "Material", "Buyer", "Qty (kg)", "Price/kg (₱)", "Total (₱)", "Invoice #", "Date" })
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
                            table.Cell().Element(CellStyle).Text($"₱{s.PricePerKg:N2}");
                            table.Cell().Element(CellStyle).Text($"₱{s.TotalAmount:N2}");
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
        public static byte[] GenerateOverallReport(List<Inventory> inventory, List<TeardownBatch> teardowns, List<CommoditySale> sales, List<ProcurementRequest> procurement)
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
                        col.Item().Text($"Total Sales Revenue: ₱{totalRevenue:N2}");
                        col.Item().Text($"Total Procurement Records: {procurement.Count}");
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
                            foreach (var h in new[] { "ID", "Material", "Buyer", "Qty (kg)", "Price/kg (₱)", "Total (₱)", "Invoice #", "Date" })
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
                            table.Cell().Element(CellStyle).Text($"₱{s.PricePerKg:N2}");
                            table.Cell().Element(CellStyle).Text($"₱{s.TotalAmount:N2}");
                            table.Cell().Element(CellStyle).Text(s.InvoiceNumber);
                            table.Cell().Element(CellStyle).Text(s.SaleDate.ToString("MMM dd, yyyy"));

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                // --- Procurement section ---
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8.5f));

                    page.Header().Text("Procurement History").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(2.0f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(2.0f);
                            c.RelativeColumn(2.0f);
                            c.RelativeColumn(2.0f);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Date", "Supplier / Company", "Device", "Qty", "Cost/Unit (₱)", "Total Cost (₱)", "Status", "Requested By", "Accepted By", "Tech Assigned" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in procurement)
                        {
                            var reqBy = !string.IsNullOrWhiteSpace(item.RequestedByFullName) ? item.RequestedByFullName : (item.RequestedByUserName ?? "—");
                            var revBy = !string.IsNullOrWhiteSpace(item.ReviewedByFullName) ? item.ReviewedByFullName : (item.ReviewedByUserName ?? "—");
                            var tech = !string.IsNullOrWhiteSpace(item.AssignedTechStaffFullName) ? item.AssignedTechStaffFullName : (item.AssignedTechStaffUserName ?? "—");

                            table.Cell().Element(CellStyle).Text(item.Id.ToString());
                            table.Cell().Element(CellStyle).Text(item.RequestedAtUtc.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellStyle).Text(item.SupplierCompany);
                            table.Cell().Element(CellStyle).Text(item.DeviceName);
                            table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                            table.Cell().Element(CellStyle).Text($"₱{item.CostPerDevice:N2}");
                            table.Cell().Element(CellStyle).Text($"₱{item.TotalCost:N2}");
                            table.Cell().Element(CellStyle).Text(item.Status.ToString());
                            table.Cell().Element(CellStyle).Text(reqBy);
                            table.Cell().Element(CellStyle).Text(revBy);
                            table.Cell().Element(CellStyle).Text(tech);

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }

        public static byte[] GenerateProcurementReport(List<ProcurementRequest> items)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(8.5f));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Procurement History Report").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Generated: {DateTime.Now:MMMM dd, yyyy h:mm tt}").FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().Text($"Total records: {items.Count}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(2.4f);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(0.8f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.8f);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "ID", "Date", "Supplier / Company", "Device", "Category", "Qty", "Cost/Unit (₱)", "Total Cost (₱)", "Status", "Requested By", "Accepted By", "Tech Assigned" })
                                header.Cell().Element(HeaderStyle).Text(h);

                            static IContainer HeaderStyle(IContainer c) =>
                                c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                        });

                        foreach (var item in items)
                        {
                            var reqBy = !string.IsNullOrWhiteSpace(item.RequestedByFullName) ? item.RequestedByFullName : (item.RequestedByUserName ?? "—");
                            var revBy = !string.IsNullOrWhiteSpace(item.ReviewedByFullName) ? item.ReviewedByFullName : (item.ReviewedByUserName ?? "—");
                            var tech = !string.IsNullOrWhiteSpace(item.AssignedTechStaffFullName) ? item.AssignedTechStaffFullName : (item.AssignedTechStaffUserName ?? "—");

                            table.Cell().Element(CellStyle).Text(item.Id.ToString());
                            table.Cell().Element(CellStyle).Text(item.RequestedAtUtc.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellStyle).Text(item.SupplierCompany);
                            table.Cell().Element(CellStyle).Text(item.DeviceName);
                            table.Cell().Element(CellStyle).Text(item.DeviceCategory?.Name ?? "—");
                            table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                            table.Cell().Element(CellStyle).Text($"₱{item.CostPerDevice:N2}");
                            table.Cell().Element(CellStyle).Text($"₱{item.TotalCost:N2}");
                            table.Cell().Element(CellStyle).Text(item.Status.ToString());
                            table.Cell().Element(CellStyle).Text(reqBy);
                            table.Cell().Element(CellStyle).Text(revBy);
                            table.Cell().Element(CellStyle).Text(tech);

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                        x.Span("S.C.R.A.P — System for Component Recovery and Processing").FontSize(8).FontColor(Colors.Grey.Darken1));
                });
            }).GeneratePdf();
        }

        public static byte[] GeneratePlatformSummaryReport(PlatformSummaryReport summary)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("S.C.R.A.P — Platform Reports & Tenancy Analytics")
                                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text("Executive tenancy breakdown, recurring subscription revenue, and multi-tenant hosting inventory")
                                    .FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                            });

                            row.ConstantItem(230).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy h:mm tt}")
                                    .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                                c.Item().Text("CONFIDENTIAL • SUPERADMIN")
                                    .FontSize(8f).Bold().FontColor(Colors.Indigo.Medium);
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Colors.Blue.Darken2);
                    });

                    // Content
                    page.Content().PaddingTop(12).Column(mainCol =>
                    {
                        // Metric Cards Row
                        mainCol.Item().Row(r =>
                        {
                            r.Spacing(8);

                            // Total Revenue Collected Card
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL REVENUE COLLECTED").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"₱{summary.TotalCollectedRevenue:N2}").FontSize(13).Bold().FontColor(Colors.Teal.Darken2);
                                c.Item().Text("All-time subscription income").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            });

                            // MRR Card
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("EST. MONTHLY REVENUE").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"₱{summary.EstimatedMonthlyRevenue:N2}").FontSize(13).Bold().FontColor(Colors.Green.Darken2);
                                c.Item().Text($"Annual: ₱{(summary.EstimatedMonthlyRevenue * 12):N2}").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            });

                            // Total Subscribers Card
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("TOTAL SUBSCRIBERS").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{summary.TotalSubscribers}").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text("Registered company tenants").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            });

                            // Active / Suspended Card
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("TENANCY HEALTH").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{summary.ActiveSubscribers} Active").FontSize(13).Bold().FontColor(Colors.Indigo.Medium);
                                c.Item().Text($"{summary.SuspendedSubscribers} Suspended accounts").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            });

                            // Tier Distribution Card
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten4).Padding(8).Column(c =>
                            {
                                c.Item().Text("PLAN DISTRIBUTION").FontSize(7.5f).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"B: {summary.BasicCount} | S: {summary.StandardCount} | E: {summary.EnterpriseCount}").FontSize(10.5f).Bold().FontColor(Colors.Grey.Darken3);
                                c.Item().Text("Basic (₱99) / Std (₱249) / Ent (₱599)").FontSize(7f).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        // Section Title: Tenant Subscriptions
                        mainCol.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Text("Registered Tenant Companies & Subscription Statuses").FontSize(10.5f).Bold().FontColor(Colors.Grey.Darken3);
                            r.ConstantItem(150).AlignRight().Text($"Total Tenants: {summary.Tenants?.Count ?? 0}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });

                        // Table 1: Tenants
                        mainCol.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.7f); // Code
                                c.RelativeColumn(1.8f); // Company Name
                                c.RelativeColumn(0.9f); // Plan
                                c.RelativeColumn(0.8f); // Status
                                c.RelativeColumn(0.9f); // Monthly Rate
                                c.RelativeColumn(1.0f); // Total Generated
                                c.RelativeColumn(0.7f); // Days Left
                                c.RelativeColumn(1.6f); // Local DB
                                c.RelativeColumn(1.6f); // Cloud DB
                                c.RelativeColumn(0.9f); // Registered
                                c.RelativeColumn(0.9f); // Expires
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Code", "Company Name", "Plan", "Status", "Rate/mo", "Total Gen", "Days Left", "Local Database", "Cloud Database", "Registered", "Expires" })
                                {
                                    header.Cell().Element(HeaderStyle).Text(h);
                                }

                                static IContainer HeaderStyle(IContainer container) =>
                                    container.Background(Colors.Blue.Darken2)
                                             .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(7.5f))
                                             .PaddingVertical(4)
                                             .PaddingHorizontal(3);
                            });

                            if (summary.Tenants != null)
                            {
                                int idx = 0;
                                foreach (var t in summary.Tenants)
                                {
                                    var bg = (idx % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.CompanyCode ?? "");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.CompanyName ?? "").Bold();
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.SubscriptionPlan ?? "");

                                    // Status cell with color
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.StatusText)
                                        .FontColor(t.IsActive ? Colors.Green.Darken2 : Colors.Red.Medium).Bold();

                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.MonthlyRateFormatted);
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.TotalMoneyGeneratedFormatted).Bold();
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.DaysRemaining.HasValue ? $"{t.DaysRemaining}d" : "—");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.DatabaseName ?? "N/A");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.CloudDatabaseName ?? "N/A");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.CreatedAt.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(t.SubscriptionExpiresAt.HasValue ? t.SubscriptionExpiresAt.Value.ToString("yyyy-MM-dd") : "N/A");

                                    idx++;
                                }
                            }

                            static IContainer CellStyle(IContainer c, string bg) =>
                                c.Background(bg)
                                 .PaddingVertical(3)
                                 .PaddingHorizontal(3)
                                 .BorderBottom(0.5f)
                                 .BorderColor(Colors.Grey.Lighten2)
                                 .DefaultTextStyle(x => x.FontSize(7.5f));
                        });

                        // Section Title: Subscription History Ledger
                        mainCol.Item().PaddingTop(14).Row(r =>
                        {
                            r.RelativeItem().Text("Subscription History & Payment Ledger").FontSize(10.5f).Bold().FontColor(Colors.Grey.Darken3);
                            r.ConstantItem(150).AlignRight().Text($"Total Records: {summary.SubscriptionHistories?.Count ?? 0}").FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                        });

                        // Table 2: Subscription History
                        mainCol.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(0.9f); // Date
                                c.RelativeColumn(2.0f); // Company
                                c.RelativeColumn(1.0f); // Plan
                                c.RelativeColumn(0.9f); // Cycle
                                c.RelativeColumn(1.0f); // Amount (₱)
                                c.RelativeColumn(0.8f); // Status
                                c.RelativeColumn(1.8f); // Period
                                c.RelativeColumn(2.4f); // Notes
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Date", "Company", "Plan", "Cycle", "Amount", "Status", "Coverage Period", "Notes" })
                                {
                                    header.Cell().Element(HeaderStyle).Text(h);
                                }

                                static IContainer HeaderStyle(IContainer container) =>
                                    container.Background(Colors.Indigo.Darken2)
                                             .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(7.5f))
                                             .PaddingVertical(4)
                                             .PaddingHorizontal(3);
                            });

                            if (summary.SubscriptionHistories != null && summary.SubscriptionHistories.Count > 0)
                            {
                                int idx = 0;
                                foreach (var h in summary.SubscriptionHistories)
                                {
                                    var bg = (idx % 2 == 0) ? Colors.White : Colors.Grey.Lighten4;

                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.CreatedAt.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(c => CellStyle(c, bg)).Text($"{h.CompanyName} ({h.CompanyCode})").Bold();
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.PlanName ?? "—");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.BillingCycle ?? "Monthly");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.AmountFormatted).Bold().FontColor(Colors.Green.Darken2);
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.Status ?? "Paid");
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.PeriodFormatted);
                                    table.Cell().Element(c => CellStyle(c, bg)).Text(h.Notes ?? "—");

                                    idx++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(8).Element(c => CellStyle(c, Colors.White)).AlignCenter().Text("No subscription transaction history recorded yet.").Italic();
                            }

                            static IContainer CellStyle(IContainer c, string bg) =>
                                c.Background(bg)
                                 .PaddingVertical(3)
                                 .PaddingHorizontal(3)
                                 .BorderBottom(0.5f)
                                 .BorderColor(Colors.Grey.Lighten2)
                                 .DefaultTextStyle(x => x.FontSize(7.5f));
                        });
                    });

                    // Footer
                    page.Footer().Column(col =>
                    {
                        col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text("S.C.R.A.P Multi-Tenant Enterprise Resource Platform — SuperAdmin Executive Report")
                                .FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Colors.Grey.Darken1));
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                        });
                    });
                });
            }).GeneratePdf();
        }
    }
}