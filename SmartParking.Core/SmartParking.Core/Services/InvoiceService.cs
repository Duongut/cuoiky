using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SmartParking.Core.Data;
using SmartParking.Core.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class InvoiceService
    {
        private readonly MongoDBContext _context;
        private readonly ParkingService _parkingService;
        private readonly IConfiguration _configuration;
        private readonly string _invoiceDirectory;

        public InvoiceService(MongoDBContext context, ParkingService parkingService, IConfiguration configuration)
        {
            _context = context;
            _parkingService = parkingService;
            _configuration = configuration;

            // Create directory for storing invoices if it doesn't exist
            _invoiceDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Invoices");
            if (!Directory.Exists(_invoiceDirectory))
            {
                Directory.CreateDirectory(_invoiceDirectory);
            }
        }

        public async Task<string> GenerateInvoiceAsync(string transactionId)
        {
            // Get transaction details
            var transaction = await _context.Transactions.Find(t => t.TransactionId == transactionId).FirstOrDefaultAsync();
            if (transaction == null)
            {
                throw new Exception($"Transaction with ID {transactionId} not found");
            }

            // Get vehicle details
            var vehicle = await _parkingService.GetVehicleById(transaction.VehicleId);
            if (vehicle == null)
            {
                throw new Exception($"Vehicle with ID {transaction.VehicleId} not found");
            }

            // Generate invoice file name
            string fileName = $"Invoice_{transaction.TransactionId}.pdf";
            string filePath = Path.Combine(_invoiceDirectory, fileName);

            // Create PDF document
            using (var writer = new PdfWriter(filePath))
            {
                using (var pdf = new PdfDocument(writer))
                {
                    using (var document = new Document(pdf))
                    {
                        // Set fonts
                        PdfFont headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                        PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                        // Add header
                        document.Add(new Paragraph("SMART PARKING SYSTEM")
                            .SetFont(headerFont)
                            .SetFontSize(20)
                            .SetTextAlignment(TextAlignment.CENTER));

                        document.Add(new Paragraph("PAYMENT INVOICE")
                            .SetFont(headerFont)
                            .SetFontSize(16)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginBottom(20));

                        // Add invoice details
                        Table infoTable = new Table(2).UseAllAvailableWidth();

                        // Invoice information
                        AddTableRow(infoTable, "Invoice Number:", transaction.TransactionId, headerFont, normalFont);
                        AddTableRow(infoTable, "Date:", transaction.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"), headerFont, normalFont);
                        AddTableRow(infoTable, "Payment Method:", GetPaymentMethodName(transaction.PaymentMethod), headerFont, normalFont);
                        AddTableRow(infoTable, "Status:", transaction.Status, headerFont, normalFont);

                        document.Add(infoTable);

                        // Vehicle information
                        document.Add(new Paragraph("Vehicle Information")
                            .SetFont(headerFont)
                            .SetFontSize(14)
                            .SetMarginTop(15)
                            .SetMarginBottom(10));

                        Table vehicleTable = new Table(2).UseAllAvailableWidth();
                        AddTableRow(vehicleTable, "Vehicle ID:", vehicle.VehicleId, headerFont, normalFont);
                        AddTableRow(vehicleTable, "License Plate:", vehicle.LicensePlate, headerFont, normalFont);
                        AddTableRow(vehicleTable, "Vehicle Type:", vehicle.VehicleType, headerFont, normalFont);
                        AddTableRow(vehicleTable, "Entry Time:", vehicle.EntryTime.ToString("dd/MM/yyyy HH:mm:ss"), headerFont, normalFont);

                        if (vehicle.ExitTime.HasValue)
                        {
                            AddTableRow(vehicleTable, "Exit Time:", vehicle.ExitTime.Value.ToString("dd/MM/yyyy HH:mm:ss"), headerFont, normalFont);
                        }

                        document.Add(vehicleTable);

                        // Payment details
                        document.Add(new Paragraph("Payment Details")
                            .SetFont(headerFont)
                            .SetFontSize(14)
                            .SetMarginTop(15)
                            .SetMarginBottom(10));

                        // Create table for payment details
                        Table table = new Table(new float[] { 3, 2 })
                            .SetWidth(UnitValue.CreatePercentValue(100))
                            .SetMarginTop(10);

                        // Add table headers
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Description").SetFont(headerFont)));
                        table.AddHeaderCell(new Cell().Add(new Paragraph("Amount (VND)").SetFont(headerFont)));

                        // Add table rows
                        table.AddCell(new Cell().Add(new Paragraph(transaction.Description).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(FormatCurrency(transaction.Amount)).SetFont(normalFont).SetTextAlignment(TextAlignment.RIGHT)));

                        // Add total row
                        table.AddCell(new Cell().Add(new Paragraph("Total").SetFont(headerFont)));
                        table.AddCell(new Cell().Add(new Paragraph(FormatCurrency(transaction.Amount)).SetFont(headerFont).SetTextAlignment(TextAlignment.RIGHT)));

                        document.Add(table);

                        // Add payment details
                        if (transaction.PaymentDetails != null)
                        {
                            Table detailsTable = new Table(2).UseAllAvailableWidth().SetMarginTop(10);

                            if (!string.IsNullOrEmpty(transaction.PaymentDetails.CashierName))
                            {
                                AddTableRow(detailsTable, "Cashier:", transaction.PaymentDetails.CashierName, headerFont, normalFont);
                            }

                            if (!string.IsNullOrEmpty(transaction.PaymentDetails.MomoTransactionId))
                            {
                                AddTableRow(detailsTable, "Momo Transaction ID:", transaction.PaymentDetails.MomoTransactionId, headerFont, normalFont);
                            }

                            if (!string.IsNullOrEmpty(transaction.PaymentDetails.StripePaymentIntentId))
                            {
                                AddTableRow(detailsTable, "Stripe Payment ID:", transaction.PaymentDetails.StripePaymentIntentId, headerFont, normalFont);

                                if (!string.IsNullOrEmpty(transaction.PaymentDetails.CardLast4))
                                {
                                    AddTableRow(detailsTable, "Card:", $"**** **** **** {transaction.PaymentDetails.CardLast4}", headerFont, normalFont);
                                }
                            }

                            if (transaction.PaymentDetails.PaymentTime.HasValue)
                            {
                                AddTableRow(detailsTable, "Payment Time:", transaction.PaymentDetails.PaymentTime.Value.ToString("dd/MM/yyyy HH:mm:ss"), headerFont, normalFont);
                            }

                            document.Add(detailsTable);
                        }

                        // Add footer
                        document.Add(new Paragraph("Thank you for using Smart Parking System!")
                            .SetFont(normalFont)
                            .SetFontSize(12)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(50));

                        document.Add(new Paragraph("This is a computer-generated invoice and does not require a signature.")
                            .SetFont(normalFont)
                            .SetFontSize(10)
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetMarginTop(5));
                    }
                }
            }

            // Update transaction with invoice URL
            string invoiceUrl = $"/Invoices/{fileName}";
            var filter = Builders<Transaction>.Filter.Eq(t => t.TransactionId, transactionId);
            var update = Builders<Transaction>.Update.Set(t => t.InvoiceUrl, invoiceUrl);
            await _context.Transactions.UpdateOneAsync(filter, update);

            return invoiceUrl;
        }

        private void AddTableRow(Table table, string label, string value, PdfFont labelFont, PdfFont valueFont)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(labelFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFont(valueFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        }

        private string GetPaymentMethodName(string paymentMethod)
        {
            return paymentMethod switch
            {
                "CASH" => "Cash",
                "MOMO" => "MoMo E-Wallet",
                "STRIPE" => "Credit/Debit Card",
                _ => paymentMethod
            };
        }

        private string FormatCurrency(decimal amount)
        {
            return $"{amount:N0}";
        }

        public string GetInvoiceFilePath(string fileName)
        {
            return Path.Combine(_invoiceDirectory, fileName);
        }
    }
}
