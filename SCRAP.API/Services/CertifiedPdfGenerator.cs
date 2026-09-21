using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SCRAP.domain.entities;

namespace SCRAP.API.Services
{
    public static class CertificatePdfGenerator
    {
        public static byte[] Generate(CertificateOfDestruction cert)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Certificate of Destruction")
                            .FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                        col.Item().Text($"Certificate #: {cert.CertificateNumber}")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Organization").Bold();
                                c.Item().Text(cert.OrganizationName);
                                c.Item().Text(cert.OrganizationAddress);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Destruction Provider").Bold();
                                c.Item().Text(cert.ProviderName);
                                c.Item().Text(cert.ProviderAddress);
                            });
                        });

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Date & Time: {cert.DestructionDateTime:MMMM dd, yyyy h:mm tt}");
                            row.RelativeItem().Text($"Method: {cert.Method}");
                        });
                        col.Item().Text($"Security Standard: {cert.SecurityStandard}");

                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("Asset Inventory").Bold().FontSize(14);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Serial Number");
                                header.Cell().Element(HeaderStyle).Text("Device Type");
                                header.Cell().Element(HeaderStyle).Text("Model");

                                static IContainer HeaderStyle(IContainer c) =>
                                    c.DefaultTextStyle(x => x.Bold()).PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                            });

                            foreach (var item in cert.Items)
                            {
                                table.Cell().Element(CellStyle).Text(item.SerialNumber);
                                table.Cell().Element(CellStyle).Text(item.DeviceType);
                                table.Cell().Element(CellStyle).Text(item.Model);
                            }

                            static IContainer CellStyle(IContainer c) =>
                                c.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        });

                        col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Verified By").Bold();
                                c.Item().Text(cert.VerifiedByName);
                                c.Item().Text($"Date: {cert.VerifiedDate:MMMM dd, yyyy}");
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(cert.Notes))
                        {
                            col.Item().PaddingTop(10).Text($"Notes: {cert.Notes}");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by S.C.R.A.P — System for Component Recovery and Processing")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}