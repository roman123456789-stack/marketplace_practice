using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;

namespace marketplace_practice.Services
{
    public class PDFService : IDocument, IPDFService
    {
        private ReceiptModel? _receiptModel;
        private readonly IFileUploadService _fileUploadService;

        public PDFService(IFileUploadService fileUploadService)
        {
            _fileUploadService = fileUploadService;
        }

        public IPDFService WithReceipt(ReceiptModel model)
        {
            _receiptModel = model;
            return this;
        }

        public void Compose(IDocumentContainer container)
        {
            if (_receiptModel == null)
                throw new InvalidOperationException("ReceiptModel is null");

            var receipt = _receiptModel;

            if (receipt.Items == null)
                throw new InvalidOperationException("Receipt.Items is null");

            // Конвертируем UTC время в московское время
            var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            var localIssueDate = TimeZoneInfo.ConvertTimeFromUtc(receipt.IssueDate, moscowTimeZone);

            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.Size(PageSizes.A5);

                    page.Content().Column(column =>
                    {
                        column.Item().AlignCenter().Text(receipt.StoreName)
                            .FontSize(24).Bold().FontColor(Colors.Blue.Medium);

                        column.Item().PaddingVertical(10) // ← Отступ именно здесь
                        .AlignCenter()
                        .Text($"Чек №{receipt.ReceiptNumber}")
                        .FontSize(16)
                        .SemiBold();

                        column.Item().Text($"Дата: {localIssueDate:dd.MM.yyyy HH:mm} (МСК)")
                            .FontSize(12).AlignCenter();

                        column.Item().PaddingVertical(10).Text($"Покупатель: {receipt.CustomerName}")
                            .FontSize(12).AlignCenter();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Товар");
                                header.Cell().Element(CellStyle).Text("Кол-во");
                                header.Cell().Element(CellStyle).Text("Цена");
                                header.Cell().Element(CellStyle).Text("Сумма");

                                static IContainer CellStyle(IContainer container) => container
                                    .Background(Colors.Grey.Lighten2)
                                    .Padding(5)
                                    .BorderBottom(1)
                                    .BorderColor(Colors.Black);
                            });

                            int index = 1;
                            foreach (var item in receipt.Items)
                            {
                                table.Cell().Element(CellStyle).Text(index++.ToString());
                                table.Cell().Element(CellStyle).Text(item.ProductName);
                                table.Cell().Element(CellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).Text($"{item.UnitPrice:F2} ₽");
                                table.Cell().Element(CellStyle).Text($"{item.Total:F2} ₽");
                            }

                            // Метод ДОЛЖЕН возвращать IContainer!
                            static IContainer CellStyle(IContainer container) => container
                                .Padding(5)
                                .BorderBottom(1)
                                .BorderColor(Colors.Grey.Lighten1);
                        });

                        column.Item().PaddingVertical(20).AlignRight().Text($"Итого: {receipt.TotalAmount:F2} ₽")
                            .FontSize(16).Bold();

                        column.Item().PaddingVertical(30).AlignCenter().Text("Спасибо за покупку!")
                            .FontSize(14).Italic().FontColor(Colors.Green.Darken2);
                    });
                });
        }

        public byte[] GeneratePdf()
        {
            if (this._receiptModel == null)
                throw new InvalidOperationException("Невозможно сгенерировать PDF: ReceiptModel не установлен.");

            // ПРАВИЛЬНЫЙ способ генерации PDF в QuestPDF
            return Document.Create(container =>
            {
                Compose(container);
            }).GeneratePdf();
        }

        public async Task<(string, byte[])> SaveReceiptAsPdfAsync(ReceiptModel model, string subPath = "receipts")
        {
            WithReceipt(model);

            try
            {
                var bytes = GeneratePdf();

                // ДОБАВЬТЕ ПРОВЕРКИ
                if (bytes == null || bytes.Length == 0)
                    throw new InvalidOperationException("PDF generation failed - empty result");

                using var stream = new MemoryStream(bytes);

                var fileName = $"receipt_{model.ReceiptNumber}.pdf";
                var formFile = new FormFile(stream, 0, bytes.Length, "file", "document.pdf")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };

                var fileUrl = await _fileUploadService.SaveFileAsync(formFile, subPath);

                return (fileUrl, bytes);
            }
            finally
            {
                _receiptModel = null;
            }
        }
    }
}