using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartParking.Core.Models;
using SmartParking.Core.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using ClosedXML.Excel;

namespace SmartParking.Core.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly TransactionService _transactionService;
        private readonly ParkingService _parkingService;
        private readonly ILogger<ReportController> _logger;
        private readonly string _reportsDirectory;
        private readonly MonthlyVehicleService _monthlyVehicleService;

        public ReportController(
            TransactionService transactionService,
            ParkingService parkingService,
            ILogger<ReportController> logger,
            MonthlyVehicleService monthlyVehicleService)
        {
            _transactionService = transactionService;
            _parkingService = parkingService;
            _logger = logger;
            _monthlyVehicleService = monthlyVehicleService;

            // Create directory for reports if it doesn't exist
            _reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
            if (!Directory.Exists(_reportsDirectory))
            {
                Directory.CreateDirectory(_reportsDirectory);
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? paymentMethod)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get transactions
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                // Filter by payment method if provided
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                {
                    transactions = transactions.Where(t => t.PaymentMethod.ToUpper() == paymentMethod.ToUpper()).ToList();
                }

                // Calculate summary
                var summary = new
                {
                    TotalTransactions = transactions.Count,
                    CompletedTransactions = transactions.Count(t => t.Status == "COMPLETED"),
                    PendingTransactions = transactions.Count(t => t.Status == "PENDING"),
                    TotalAmount = transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount),
                    PaymentMethods = new
                    {
                        Cash = transactions.Where(t => t.PaymentMethod == "CASH" && t.Status == "COMPLETED").Sum(t => t.Amount),
                        Momo = transactions.Where(t => t.PaymentMethod == "MOMO" && t.Status == "COMPLETED").Sum(t => t.Amount),
                        Stripe = transactions.Where(t => t.PaymentMethod == "STRIPE" && t.Status == "COMPLETED").Sum(t => t.Amount)
                    }
                };

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    summary = summary,
                    transactions = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transaction report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue by payment method
                var revenueByPaymentMethod = await _transactionService.GetRevenueByPaymentMethodAsync(start, end);

                // Get revenue by vehicle type
                var revenueByVehicleType = await _transactionService.GetRevenueByVehicleTypeAsync(start, end);

                // Get transactions for daily revenue calculation
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);
                var completedTransactions = transactions.Where(t => t.Status == "COMPLETED").ToList();

                // Calculate daily revenue
                var dailyRevenue = completedTransactions
                    .GroupBy(t => t.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(t => t.Amount),
                        Cash = g.Where(t => t.PaymentMethod == "CASH").Sum(t => t.Amount),
                        Momo = g.Where(t => t.PaymentMethod == "MOMO").Sum(t => t.Amount),
                        Stripe = g.Where(t => t.PaymentMethod == "STRIPE").Sum(t => t.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    revenueByPaymentMethod = revenueByPaymentMethod,
                    revenueByVehicleType = revenueByVehicleType,
                    dailyRevenue = dailyRevenue,
                    totalRevenue = revenueByPaymentMethod["TOTAL"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating revenue report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/transactions/pdf")]
        public async Task<IActionResult> ExportTransactionsToPdf([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? paymentMethod)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get transactions
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                // Filter by payment method if provided
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                {
                    transactions = transactions.Where(t => t.PaymentMethod.ToUpper() == paymentMethod.ToUpper()).ToList();
                }

                // Generate PDF file name
                string fileName = $"Transactions_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create PDF document
                using (var writer = new PdfWriter(filePath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        using (var document = new Document(pdf))
                        {
                            // Set fonts
                            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                            // Add header
                            document.Add(new Paragraph("SMART PARKING SYSTEM")
                                .SetFont(boldFont)
                                .SetFontSize(20)
                                .SetTextAlignment(TextAlignment.CENTER));

                            document.Add(new Paragraph("TRANSACTION REPORT")
                                .SetFont(boldFont)
                                .SetFontSize(16)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20));

                            // Add report details
                            document.Add(new Paragraph($"Period: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}")
                                .SetFont(normalFont)
                                .SetFontSize(12)
                                .SetMarginBottom(10));

                            if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                            {
                                document.Add(new Paragraph($"Payment Method: {paymentMethod}")
                                    .SetFont(normalFont)
                                    .SetFontSize(12)
                                    .SetMarginBottom(10));
                            }

                            // Add summary
                            document.Add(new Paragraph("Summary")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginBottom(10));

                            Table summaryTable = new Table(2).UseAllAvailableWidth();
                            AddTableRow(summaryTable, "Total Transactions:", transactions.Count.ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Completed Transactions:", transactions.Count(t => t.Status == "COMPLETED").ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Pending Transactions:", transactions.Count(t => t.Status == "PENDING").ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Total Amount:", FormatCurrency(transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount)), boldFont, normalFont);

                            document.Add(summaryTable);

                            // Add payment method breakdown
                            document.Add(new Paragraph("Payment Method Breakdown")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table paymentTable = new Table(2).UseAllAvailableWidth();
                            AddTableRow(paymentTable, "Cash:", FormatCurrency(transactions.Where(t => t.PaymentMethod == "CASH" && t.Status == "COMPLETED").Sum(t => t.Amount)), boldFont, normalFont);
                            AddTableRow(paymentTable, "Momo:", FormatCurrency(transactions.Where(t => t.PaymentMethod == "MOMO" && t.Status == "COMPLETED").Sum(t => t.Amount)), boldFont, normalFont);
                            AddTableRow(paymentTable, "Stripe:", FormatCurrency(transactions.Where(t => t.PaymentMethod == "STRIPE" && t.Status == "COMPLETED").Sum(t => t.Amount)), boldFont, normalFont);

                            document.Add(paymentTable);

                            // Add transactions table
                            document.Add(new Paragraph("Transaction Details")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table table = new Table(new float[] { 2, 2, 1, 1, 1, 1 })
                                .SetWidth(UnitValue.CreatePercentValue(100));

                            // Add table headers
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Transaction ID").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Vehicle ID").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Payment").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetFont(boldFont)));

                            // Add table rows
                            foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                            {
                                table.AddCell(new Cell().Add(new Paragraph(transaction.TransactionId).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.Timestamp.ToString("dd/MM/yyyy HH:mm")).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.VehicleId).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(FormatCurrency(transaction.Amount)).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.PaymentMethod).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.Status).SetFont(normalFont)));
                            }

                            document.Add(table);

                            // Add footer
                            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                .SetFont(normalFont)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.RIGHT)
                                .SetMarginTop(20));
                        }
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions to PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/transactions/excel")]
        public async Task<IActionResult> ExportTransactionsToExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? paymentMethod)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get transactions
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                // Filter by payment method if provided
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                {
                    transactions = transactions.Where(t => t.PaymentMethod.ToUpper() == paymentMethod.ToUpper()).ToList();
                }

                // Generate Excel file name
                string fileName = $"Transactions_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    // Add summary worksheet
                    var summaryWorksheet = workbook.Worksheets.Add("Summary");

                    // Add title
                    summaryWorksheet.Cell("A1").Value = "SMART PARKING SYSTEM - TRANSACTION REPORT";
                    summaryWorksheet.Range("A1:F1").Merge();
                    summaryWorksheet.Cell("A1").Style.Font.Bold = true;
                    summaryWorksheet.Cell("A1").Style.Font.FontSize = 16;
                    summaryWorksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add report details
                    summaryWorksheet.Cell("A3").Value = "Period:";
                    summaryWorksheet.Cell("B3").Value = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                    {
                        summaryWorksheet.Cell("A4").Value = "Payment Method:";
                        summaryWorksheet.Cell("B4").Value = paymentMethod;
                    }

                    // Add summary
                    summaryWorksheet.Cell("A6").Value = "SUMMARY";
                    summaryWorksheet.Cell("A6").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A7").Value = "Total Transactions:";
                    summaryWorksheet.Cell("B7").Value = transactions.Count;

                    summaryWorksheet.Cell("A8").Value = "Completed Transactions:";
                    summaryWorksheet.Cell("B8").Value = transactions.Count(t => t.Status == "COMPLETED");

                    summaryWorksheet.Cell("A9").Value = "Pending Transactions:";
                    summaryWorksheet.Cell("B9").Value = transactions.Count(t => t.Status == "PENDING");

                    summaryWorksheet.Cell("A10").Value = "Total Amount:";
                    summaryWorksheet.Cell("B10").Value = transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount);
                    summaryWorksheet.Cell("B10").Style.NumberFormat.Format = "#,##0";

                    // Add payment method breakdown
                    summaryWorksheet.Cell("A12").Value = "PAYMENT METHOD BREAKDOWN";
                    summaryWorksheet.Cell("A12").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A13").Value = "Cash:";
                    summaryWorksheet.Cell("B13").Value = transactions.Where(t => t.PaymentMethod == "CASH" && t.Status == "COMPLETED").Sum(t => t.Amount);
                    summaryWorksheet.Cell("B13").Style.NumberFormat.Format = "#,##0";

                    summaryWorksheet.Cell("A14").Value = "Momo:";
                    summaryWorksheet.Cell("B14").Value = transactions.Where(t => t.PaymentMethod == "MOMO" && t.Status == "COMPLETED").Sum(t => t.Amount);
                    summaryWorksheet.Cell("B14").Style.NumberFormat.Format = "#,##0";

                    summaryWorksheet.Cell("A15").Value = "Stripe:";
                    summaryWorksheet.Cell("B15").Value = transactions.Where(t => t.PaymentMethod == "STRIPE" && t.Status == "COMPLETED").Sum(t => t.Amount);
                    summaryWorksheet.Cell("B15").Style.NumberFormat.Format = "#,##0";

                    // Auto-fit columns
                    summaryWorksheet.Columns().AdjustToContents();

                    // Add transactions worksheet
                    var transactionsWorksheet = workbook.Worksheets.Add("Transactions");

                    // Add headers
                    transactionsWorksheet.Cell("A1").Value = "Transaction ID";
                    transactionsWorksheet.Cell("B1").Value = "Date";
                    transactionsWorksheet.Cell("C1").Value = "Vehicle ID";
                    transactionsWorksheet.Cell("D1").Value = "Description";
                    transactionsWorksheet.Cell("E1").Value = "Amount";
                    transactionsWorksheet.Cell("F1").Value = "Payment Method";
                    transactionsWorksheet.Cell("G1").Value = "Status";

                    // Style headers
                    var headerRange = transactionsWorksheet.Range("A1:G1");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    int row = 2;
                    foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                    {
                        transactionsWorksheet.Cell(row, 1).Value = transaction.TransactionId;
                        transactionsWorksheet.Cell(row, 2).Value = transaction.Timestamp;
                        transactionsWorksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                        transactionsWorksheet.Cell(row, 3).Value = transaction.VehicleId;
                        transactionsWorksheet.Cell(row, 4).Value = transaction.Description;
                        transactionsWorksheet.Cell(row, 5).Value = transaction.Amount;
                        transactionsWorksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        transactionsWorksheet.Cell(row, 6).Value = transaction.PaymentMethod;
                        transactionsWorksheet.Cell(row, 7).Value = transaction.Status;
                        row++;
                    }

                    // Auto-fit columns
                    transactionsWorksheet.Columns().AdjustToContents();

                    // Save workbook
                    workbook.SaveAs(filePath);
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/transactions/csv")]
        public async Task<IActionResult> ExportTransactionsToCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? paymentMethod)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get transactions
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                // Filter by payment method if provided
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                {
                    transactions = transactions.Where(t => t.PaymentMethod.ToUpper() == paymentMethod.ToUpper()).ToList();
                }

                // Generate CSV file name
                string fileName = $"Transactions_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write header
                    writer.WriteLine("Transaction ID,Date,Vehicle ID,Description,Amount,Payment Method,Status");

                    // Write data
                    foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                    {
                        writer.WriteLine(
                            $"\"{transaction.TransactionId}\"," +
                            $"\"{transaction.Timestamp:dd/MM/yyyy HH:mm:ss}\"," +
                            $"\"{transaction.VehicleId}\"," +
                            $"\"{transaction.Description.Replace("\"", "\"\"")}\"," +
                            $"\"{transaction.Amount}\"," +
                            $"\"{transaction.PaymentMethod}\"," +
                            $"\"{transaction.Status}\""
                        );
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions to CSV");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/revenue/pdf")]
        public async Task<IActionResult> ExportRevenueToPdf([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var revenueByPaymentMethod = await _transactionService.GetRevenueByPaymentMethodAsync(start, end);
                var revenueByVehicleType = await _transactionService.GetRevenueByVehicleTypeAsync(start, end);
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);
                var completedTransactions = transactions.Where(t => t.Status == "COMPLETED").ToList();

                // Calculate daily revenue
                var dailyRevenue = completedTransactions
                    .GroupBy(t => t.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(t => t.Amount),
                        Cash = g.Where(t => t.PaymentMethod == "CASH").Sum(t => t.Amount),
                        Momo = g.Where(t => t.PaymentMethod == "MOMO").Sum(t => t.Amount),
                        Stripe = g.Where(t => t.PaymentMethod == "STRIPE").Sum(t => t.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Generate PDF file name
                string fileName = $"Revenue_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create PDF document
                using (var writer = new PdfWriter(filePath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        using (var document = new Document(pdf))
                        {
                            // Set fonts
                            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                            // Add header
                            document.Add(new Paragraph("SMART PARKING SYSTEM")
                                .SetFont(boldFont)
                                .SetFontSize(20)
                                .SetTextAlignment(TextAlignment.CENTER));

                            document.Add(new Paragraph("REVENUE REPORT")
                                .SetFont(boldFont)
                                .SetFontSize(16)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20));

                            // Add report details
                            document.Add(new Paragraph($"Period: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}")
                                .SetFont(normalFont)
                                .SetFontSize(12)
                                .SetMarginBottom(10));

                            // Add summary
                            document.Add(new Paragraph("Revenue Summary")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginBottom(10));

                            Table summaryTable = new Table(2).UseAllAvailableWidth();
                            AddTableRow(summaryTable, "Total Revenue:", FormatCurrency(revenueByPaymentMethod["TOTAL"]), boldFont, normalFont);
                            document.Add(summaryTable);

                            // Add payment method breakdown
                            document.Add(new Paragraph("Revenue by Payment Method")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table paymentTable = new Table(3).UseAllAvailableWidth();

                            // Add table headers
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Payment Method").SetFont(boldFont)));
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Amount (VND)").SetFont(boldFont)));
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Percentage").SetFont(boldFont)));

                            // Add table rows
                            decimal totalRevenue = revenueByPaymentMethod["TOTAL"];

                            paymentTable.AddCell(new Cell().Add(new Paragraph("Cash").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByPaymentMethod["CASH"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByPaymentMethod["CASH"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("MoMo").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByPaymentMethod["MOMO"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByPaymentMethod["MOMO"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("Stripe").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByPaymentMethod["STRIPE"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByPaymentMethod["STRIPE"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("Total").SetFont(boldFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(totalRevenue)).SetFont(boldFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph("100%").SetFont(boldFont)));

                            document.Add(paymentTable);

                            // Add vehicle type breakdown
                            document.Add(new Paragraph("Revenue by Vehicle Type")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table vehicleTable = new Table(3).UseAllAvailableWidth();

                            // Add table headers
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Vehicle Type").SetFont(boldFont)));
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Amount (VND)").SetFont(boldFont)));
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Percentage").SetFont(boldFont)));

                            // Add table rows
                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Car").SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByVehicleType["CAR"])).SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByVehicleType["CAR"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Motorcycle").SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByVehicleType["MOTORCYCLE"])).SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByVehicleType["MOTORCYCLE"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Total").SetFont(boldFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(totalRevenue)).SetFont(boldFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph("100%").SetFont(boldFont)));

                            document.Add(vehicleTable);

                            // Add daily revenue table
                            document.Add(new Paragraph("Daily Revenue")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table dailyTable = new Table(5).UseAllAvailableWidth();

                            // Add table headers
                            dailyTable.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(boldFont)));
                            dailyTable.AddHeaderCell(new Cell().Add(new Paragraph("Total (VND)").SetFont(boldFont)));
                            dailyTable.AddHeaderCell(new Cell().Add(new Paragraph("Cash (VND)").SetFont(boldFont)));
                            dailyTable.AddHeaderCell(new Cell().Add(new Paragraph("MoMo (VND)").SetFont(boldFont)));
                            dailyTable.AddHeaderCell(new Cell().Add(new Paragraph("Stripe (VND)").SetFont(boldFont)));

                            // Add table rows
                            foreach (var day in dailyRevenue)
                            {
                                dailyTable.AddCell(new Cell().Add(new Paragraph(day.Date.ToString("dd/MM/yyyy")).SetFont(normalFont)));
                                dailyTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(day.Total)).SetFont(normalFont)));
                                dailyTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(day.Cash)).SetFont(normalFont)));
                                dailyTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(day.Momo)).SetFont(normalFont)));
                                dailyTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(day.Stripe)).SetFont(normalFont)));
                            }

                            document.Add(dailyTable);

                            // Add footer
                            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                .SetFont(normalFont)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.RIGHT)
                                .SetMarginTop(20));
                        }
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting revenue to PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/revenue/excel")]
        public async Task<IActionResult> ExportRevenueToExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var revenueByPaymentMethod = await _transactionService.GetRevenueByPaymentMethodAsync(start, end);
                var revenueByVehicleType = await _transactionService.GetRevenueByVehicleTypeAsync(start, end);
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);
                var completedTransactions = transactions.Where(t => t.Status == "COMPLETED").ToList();

                // Calculate daily revenue
                var dailyRevenue = completedTransactions
                    .GroupBy(t => t.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(t => t.Amount),
                        Cash = g.Where(t => t.PaymentMethod == "CASH").Sum(t => t.Amount),
                        Momo = g.Where(t => t.PaymentMethod == "MOMO").Sum(t => t.Amount),
                        Stripe = g.Where(t => t.PaymentMethod == "STRIPE").Sum(t => t.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Generate Excel file name
                string fileName = $"Revenue_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    // Add summary worksheet
                    var summaryWorksheet = workbook.Worksheets.Add("Summary");

                    // Add title
                    summaryWorksheet.Cell("A1").Value = "SMART PARKING SYSTEM - REVENUE REPORT";
                    summaryWorksheet.Range("A1:F1").Merge();
                    summaryWorksheet.Cell("A1").Style.Font.Bold = true;
                    summaryWorksheet.Cell("A1").Style.Font.FontSize = 16;
                    summaryWorksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add report details
                    summaryWorksheet.Cell("A3").Value = "Period:";
                    summaryWorksheet.Cell("B3").Value = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";

                    // Add summary
                    summaryWorksheet.Cell("A5").Value = "REVENUE SUMMARY";
                    summaryWorksheet.Cell("A5").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A6").Value = "Total Revenue:";
                    summaryWorksheet.Cell("B6").Value = revenueByPaymentMethod["TOTAL"];
                    summaryWorksheet.Cell("B6").Style.NumberFormat.Format = "#,##0";

                    // Add payment method breakdown
                    summaryWorksheet.Cell("A8").Value = "REVENUE BY PAYMENT METHOD";
                    summaryWorksheet.Cell("A8").Style.Font.Bold = true;

                    // Add headers
                    summaryWorksheet.Cell("A9").Value = "Payment Method";
                    summaryWorksheet.Cell("B9").Value = "Amount (VND)";
                    summaryWorksheet.Cell("C9").Value = "Percentage";
                    summaryWorksheet.Range("A9:C9").Style.Font.Bold = true;
                    summaryWorksheet.Range("A9:C9").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    decimal totalRevenue = revenueByPaymentMethod["TOTAL"];

                    summaryWorksheet.Cell("A10").Value = "Cash";
                    summaryWorksheet.Cell("B10").Value = revenueByPaymentMethod["CASH"];
                    summaryWorksheet.Cell("B10").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C10").Value = totalRevenue > 0 ? revenueByPaymentMethod["CASH"] / totalRevenue : 0;
                    summaryWorksheet.Cell("C10").Style.NumberFormat.Format = "0.00%";

                    summaryWorksheet.Cell("A11").Value = "MoMo";
                    summaryWorksheet.Cell("B11").Value = revenueByPaymentMethod["MOMO"];
                    summaryWorksheet.Cell("B11").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C11").Value = totalRevenue > 0 ? revenueByPaymentMethod["MOMO"] / totalRevenue : 0;
                    summaryWorksheet.Cell("C11").Style.NumberFormat.Format = "0.00%";

                    summaryWorksheet.Cell("A12").Value = "Stripe";
                    summaryWorksheet.Cell("B12").Value = revenueByPaymentMethod["STRIPE"];
                    summaryWorksheet.Cell("B12").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C12").Value = totalRevenue > 0 ? revenueByPaymentMethod["STRIPE"] / totalRevenue : 0;
                    summaryWorksheet.Cell("C12").Style.NumberFormat.Format = "0.00%";

                    summaryWorksheet.Cell("A13").Value = "Total";
                    summaryWorksheet.Cell("B13").Value = totalRevenue;
                    summaryWorksheet.Cell("B13").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C13").Value = 1;
                    summaryWorksheet.Cell("C13").Style.NumberFormat.Format = "0.00%";
                    summaryWorksheet.Range("A13:C13").Style.Font.Bold = true;

                    // Add vehicle type breakdown
                    summaryWorksheet.Cell("A15").Value = "REVENUE BY VEHICLE TYPE";
                    summaryWorksheet.Cell("A15").Style.Font.Bold = true;

                    // Add headers
                    summaryWorksheet.Cell("A16").Value = "Vehicle Type";
                    summaryWorksheet.Cell("B16").Value = "Amount (VND)";
                    summaryWorksheet.Cell("C16").Value = "Percentage";
                    summaryWorksheet.Range("A16:C16").Style.Font.Bold = true;
                    summaryWorksheet.Range("A16:C16").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    summaryWorksheet.Cell("A17").Value = "Car";
                    summaryWorksheet.Cell("B17").Value = revenueByVehicleType["CAR"];
                    summaryWorksheet.Cell("B17").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C17").Value = totalRevenue > 0 ? revenueByVehicleType["CAR"] / totalRevenue : 0;
                    summaryWorksheet.Cell("C17").Style.NumberFormat.Format = "0.00%";

                    summaryWorksheet.Cell("A18").Value = "Motorcycle";
                    summaryWorksheet.Cell("B18").Value = revenueByVehicleType["MOTORCYCLE"];
                    summaryWorksheet.Cell("B18").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C18").Value = totalRevenue > 0 ? revenueByVehicleType["MOTORCYCLE"] / totalRevenue : 0;
                    summaryWorksheet.Cell("C18").Style.NumberFormat.Format = "0.00%";

                    summaryWorksheet.Cell("A19").Value = "Total";
                    summaryWorksheet.Cell("B19").Value = totalRevenue;
                    summaryWorksheet.Cell("B19").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C19").Value = 1;
                    summaryWorksheet.Cell("C19").Style.NumberFormat.Format = "0.00%";
                    summaryWorksheet.Range("A19:C19").Style.Font.Bold = true;

                    // Auto-fit columns
                    summaryWorksheet.Columns().AdjustToContents();

                    // Add daily revenue worksheet
                    var dailyWorksheet = workbook.Worksheets.Add("Daily Revenue");

                    // Add headers
                    dailyWorksheet.Cell("A1").Value = "Date";
                    dailyWorksheet.Cell("B1").Value = "Total (VND)";
                    dailyWorksheet.Cell("C1").Value = "Cash (VND)";
                    dailyWorksheet.Cell("D1").Value = "MoMo (VND)";
                    dailyWorksheet.Cell("E1").Value = "Stripe (VND)";

                    // Style headers
                    var headerRange = dailyWorksheet.Range("A1:E1");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    int row = 2;
                    foreach (var day in dailyRevenue)
                    {
                        dailyWorksheet.Cell(row, 1).Value = day.Date;
                        dailyWorksheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
                        dailyWorksheet.Cell(row, 2).Value = day.Total;
                        dailyWorksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                        dailyWorksheet.Cell(row, 3).Value = day.Cash;
                        dailyWorksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        dailyWorksheet.Cell(row, 4).Value = day.Momo;
                        dailyWorksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                        dailyWorksheet.Cell(row, 5).Value = day.Stripe;
                        dailyWorksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        row++;
                    }

                    // Auto-fit columns
                    dailyWorksheet.Columns().AdjustToContents();

                    // Save workbook
                    workbook.SaveAs(filePath);
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting revenue to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/revenue/csv")]
        public async Task<IActionResult> ExportRevenueToCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);
                var completedTransactions = transactions.Where(t => t.Status == "COMPLETED").ToList();

                // Calculate daily revenue
                var dailyRevenue = completedTransactions
                    .GroupBy(t => t.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(t => t.Amount),
                        Cash = g.Where(t => t.PaymentMethod == "CASH").Sum(t => t.Amount),
                        Momo = g.Where(t => t.PaymentMethod == "MOMO").Sum(t => t.Amount),
                        Stripe = g.Where(t => t.PaymentMethod == "STRIPE").Sum(t => t.Amount)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Generate CSV file name
                string fileName = $"Revenue_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write header
                    writer.WriteLine("Date,Total (VND),Cash (VND),MoMo (VND),Stripe (VND)");

                    // Write data
                    foreach (var day in dailyRevenue)
                    {
                        writer.WriteLine(
                            $"\"{day.Date:dd/MM/yyyy}\"," +
                            $"\"{day.Total}\"," +
                            $"\"{day.Cash}\"," +
                            $"\"{day.Momo}\"," +
                            $"\"{day.Stripe}\""
                        );
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting revenue to CSV");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("monthly-subscriptions")]
        public async Task<IActionResult> GetMonthlySubscriptionReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? vehicleType)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get monthly subscription transactions
                var transactions = await _transactionService.GetMonthlySubscriptionTransactionsAsync(start, end);

                // Filter by vehicle type if provided
                if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                {
                    if (vehicleType.ToUpper() == "CAR")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("C")).ToList();
                    }
                    else if (vehicleType.ToUpper() == "MOTORCYCLE")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("M")).ToList();
                    }
                }

                // Calculate summary
                var summary = new
                {
                    TotalTransactions = transactions.Count,
                    CompletedTransactions = transactions.Count(t => t.Status == "COMPLETED"),
                    PendingTransactions = transactions.Count(t => t.Status == "PENDING"),
                    TotalAmount = transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount),
                    NewSubscriptions = transactions.Count(t => t.Type == "MONTHLY_SUBSCRIPTION"),
                    Renewals = transactions.Count(t => t.Type == "MONTHLY_RENEWAL"),
                    NewSubscriptionAmount = transactions.Where(t => t.Type == "MONTHLY_SUBSCRIPTION" && t.Status == "COMPLETED").Sum(t => t.Amount),
                    RenewalAmount = transactions.Where(t => t.Type == "MONTHLY_RENEWAL" && t.Status == "COMPLETED").Sum(t => t.Amount),
                    PaymentMethods = new
                    {
                        Cash = transactions.Where(t => t.PaymentMethod == "CASH" && t.Status == "COMPLETED").Sum(t => t.Amount),
                        Momo = transactions.Where(t => t.PaymentMethod == "MOMO" && t.Status == "COMPLETED").Sum(t => t.Amount),
                        Stripe = transactions.Where(t => t.PaymentMethod == "STRIPE" && t.Status == "COMPLETED").Sum(t => t.Amount)
                    },
                    VehicleTypes = new
                    {
                        Car = transactions.Where(t => t.VehicleId.StartsWith("C") && t.Status == "COMPLETED").Sum(t => t.Amount),
                        Motorcycle = transactions.Where(t => t.VehicleId.StartsWith("M") && t.Status == "COMPLETED").Sum(t => t.Amount)
                    }
                };

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    summary = summary,
                    transactions = transactions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly subscription report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("monthly-subscription-revenue")]
        public async Task<IActionResult> GetMonthlySubscriptionRevenueReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var revenueByCategory = await _transactionService.GetMonthlySubscriptionRevenueAsync(start, end);
                var dailyRevenue = await _transactionService.GetDailyMonthlySubscriptionRevenueAsync(start, end);

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    revenueByCategory = revenueByCategory,
                    dailyRevenue = dailyRevenue,
                    totalRevenue = revenueByCategory["TOTAL"]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly subscription revenue report");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/monthly-subscriptions/pdf")]
        public async Task<IActionResult> ExportMonthlySubscriptionsToPdf([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? vehicleType)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get monthly subscription transactions
                var transactions = await _transactionService.GetMonthlySubscriptionTransactionsAsync(start, end);

                // Filter by vehicle type if provided
                if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                {
                    if (vehicleType.ToUpper() == "CAR")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("C")).ToList();
                    }
                    else if (vehicleType.ToUpper() == "MOTORCYCLE")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("M")).ToList();
                    }
                }

                // Generate PDF file name
                string fileName = $"MonthlySubscriptions_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create PDF document
                using (var writer = new PdfWriter(filePath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        using (var document = new Document(pdf))
                        {
                            // Set fonts
                            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                            // Add header
                            document.Add(new Paragraph("SMART PARKING SYSTEM")
                                .SetFont(boldFont)
                                .SetFontSize(20)
                                .SetTextAlignment(TextAlignment.CENTER));

                            document.Add(new Paragraph("MONTHLY SUBSCRIPTION REPORT")
                                .SetFont(boldFont)
                                .SetFontSize(16)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20));

                            // Add report details
                            document.Add(new Paragraph($"Period: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}")
                                .SetFont(normalFont)
                                .SetFontSize(12)
                                .SetMarginBottom(10));

                            if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                            {
                                document.Add(new Paragraph($"Vehicle Type: {vehicleType}")
                                    .SetFont(normalFont)
                                    .SetFontSize(12)
                                    .SetMarginBottom(10));
                            }

                            // Add summary
                            document.Add(new Paragraph("Summary")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginBottom(10));

                            Table summaryTable = new Table(2).UseAllAvailableWidth();
                            AddTableRow(summaryTable, "Total Transactions:", transactions.Count.ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Completed Transactions:", transactions.Count(t => t.Status == "COMPLETED").ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Pending Transactions:", transactions.Count(t => t.Status == "PENDING").ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Total Amount:", FormatCurrency(transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount)), boldFont, normalFont);
                            AddTableRow(summaryTable, "New Subscriptions:", transactions.Count(t => t.Type == "MONTHLY_SUBSCRIPTION").ToString(), boldFont, normalFont);
                            AddTableRow(summaryTable, "Renewals:", transactions.Count(t => t.Type == "MONTHLY_RENEWAL").ToString(), boldFont, normalFont);

                            document.Add(summaryTable);

                            // Add transactions table
                            document.Add(new Paragraph("Transaction Details")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table table = new Table(new float[] { 2, 2, 1, 1, 1, 1, 1 })
                                .SetWidth(UnitValue.CreatePercentValue(100));

                            // Add table headers
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Transaction ID").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Vehicle ID").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Type").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Payment").SetFont(boldFont)));
                            table.AddHeaderCell(new Cell().Add(new Paragraph("Status").SetFont(boldFont)));

                            // Add table rows
                            foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                            {
                                table.AddCell(new Cell().Add(new Paragraph(transaction.TransactionId).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.Timestamp.ToString("dd/MM/yyyy HH:mm")).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.VehicleId).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.Type == "MONTHLY_SUBSCRIPTION" ? "New" : "Renewal").SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(FormatCurrency(transaction.Amount)).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.PaymentMethod).SetFont(normalFont)));
                                table.AddCell(new Cell().Add(new Paragraph(transaction.Status).SetFont(normalFont)));
                            }

                            document.Add(table);

                            // Add footer
                            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                .SetFont(normalFont)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.RIGHT)
                                .SetMarginTop(20));
                        }
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscriptions to PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/monthly-subscription-revenue/pdf")]
        public async Task<IActionResult> ExportMonthlySubscriptionRevenueToPdf([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var revenueByCategory = await _transactionService.GetMonthlySubscriptionRevenueAsync(start, end);
                var dailyRevenue = await _transactionService.GetDailyMonthlySubscriptionRevenueAsync(start, end);

                // Calculate total revenue
                decimal totalRevenue = revenueByCategory["TOTAL"];

                // Generate PDF file name
                string fileName = $"MonthlySubscriptionRevenue_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create PDF document
                using (var writer = new PdfWriter(filePath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        using (var document = new Document(pdf))
                        {
                            // Set fonts
                            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                            var normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                            // Add header
                            document.Add(new Paragraph("SMART PARKING SYSTEM")
                                .SetFont(boldFont)
                                .SetFontSize(20)
                                .SetTextAlignment(TextAlignment.CENTER));

                            document.Add(new Paragraph("MONTHLY SUBSCRIPTION REVENUE REPORT")
                                .SetFont(boldFont)
                                .SetFontSize(16)
                                .SetTextAlignment(TextAlignment.CENTER)
                                .SetMarginBottom(20));

                            // Add report details
                            document.Add(new Paragraph($"Period: {start:dd/MM/yyyy} - {end:dd/MM/yyyy}")
                                .SetFont(normalFont)
                                .SetFontSize(12)
                                .SetMarginBottom(10));

                            // Add summary
                            document.Add(new Paragraph("Revenue Summary")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginBottom(10));

                            Table summaryTable = new Table(2).UseAllAvailableWidth();
                            AddTableRow(summaryTable, "Total Revenue:", FormatCurrency(revenueByCategory["TOTAL"]), boldFont, normalFont);
                            AddTableRow(summaryTable, "New Subscription Revenue:", FormatCurrency(revenueByCategory["NEW_SUBSCRIPTION"]), boldFont, normalFont);
                            AddTableRow(summaryTable, "Renewal Revenue:", FormatCurrency(revenueByCategory["RENEWAL"]), boldFont, normalFont);
                            document.Add(summaryTable);

                            // Add revenue by vehicle type
                            document.Add(new Paragraph("Revenue by Vehicle Type")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table vehicleTable = new Table(3).UseAllAvailableWidth();

                            // Add table headers
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Vehicle Type").SetFont(boldFont)));
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Amount (VND)").SetFont(boldFont)));
                            vehicleTable.AddHeaderCell(new Cell().Add(new Paragraph("Percentage").SetFont(boldFont)));

                            // Add table rows
                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Car").SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByCategory["CAR"])).SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByCategory["CAR"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Motorcycle").SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByCategory["MOTORCYCLE"])).SetFont(normalFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByCategory["MOTORCYCLE"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            vehicleTable.AddCell(new Cell().Add(new Paragraph("Total").SetFont(boldFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(totalRevenue)).SetFont(boldFont)));
                            vehicleTable.AddCell(new Cell().Add(new Paragraph("100%").SetFont(boldFont)));

                            document.Add(vehicleTable);

                            // Add payment method breakdown
                            document.Add(new Paragraph("Revenue by Payment Method")
                                .SetFont(boldFont)
                                .SetFontSize(14)
                                .SetMarginTop(15)
                                .SetMarginBottom(10));

                            Table paymentTable = new Table(3).UseAllAvailableWidth();

                            // Add table headers
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Payment Method").SetFont(boldFont)));
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Amount (VND)").SetFont(boldFont)));
                            paymentTable.AddHeaderCell(new Cell().Add(new Paragraph("Percentage").SetFont(boldFont)));

                            // Add table rows
                            paymentTable.AddCell(new Cell().Add(new Paragraph("Cash").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByCategory["CASH"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByCategory["CASH"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("MoMo").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByCategory["MOMO"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByCategory["MOMO"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("Stripe").SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(revenueByCategory["STRIPE"])).SetFont(normalFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(totalRevenue > 0 ? $"{(revenueByCategory["STRIPE"] / totalRevenue * 100):F2}%" : "0%").SetFont(normalFont)));

                            paymentTable.AddCell(new Cell().Add(new Paragraph("Total").SetFont(boldFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph(FormatCurrency(totalRevenue)).SetFont(boldFont)));
                            paymentTable.AddCell(new Cell().Add(new Paragraph("100%").SetFont(boldFont)));

                            document.Add(paymentTable);

                            // Add footer
                            document.Add(new Paragraph($"Generated on: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                .SetFont(normalFont)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.RIGHT)
                                .SetMarginTop(20));
                        }
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscription revenue to PDF");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/monthly-subscriptions/excel")]
        public async Task<IActionResult> ExportMonthlySubscriptionsToExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? vehicleType)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get monthly subscription transactions
                var transactions = await _transactionService.GetMonthlySubscriptionTransactionsAsync(start, end);

                // Filter by vehicle type if provided
                if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                {
                    if (vehicleType.ToUpper() == "CAR")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("C")).ToList();
                    }
                    else if (vehicleType.ToUpper() == "MOTORCYCLE")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("M")).ToList();
                    }
                }

                // Get vehicle details for each transaction
                var vehicleDetails = new Dictionary<string, dynamic>();
                foreach (var transaction in transactions)
                {
                    if (!vehicleDetails.ContainsKey(transaction.VehicleId))
                    {
                        var monthlyVehicle = await _monthlyVehicleService.GetMonthlyVehicleByIdAsync(transaction.VehicleId);
                        if (monthlyVehicle != null)
                        {
                            vehicleDetails[transaction.VehicleId] = monthlyVehicle;
                        }
                    }
                }

                // Generate Excel file name
                string fileName = $"MonthlySubscriptions_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    // Add summary worksheet
                    var summaryWorksheet = workbook.Worksheets.Add("Summary");

                    // Add title
                    summaryWorksheet.Cell("A1").Value = "SMART PARKING SYSTEM - MONTHLY SUBSCRIPTION TRANSACTIONS REPORT";
                    summaryWorksheet.Range("A1:F1").Merge();
                    summaryWorksheet.Cell("A1").Style.Font.Bold = true;
                    summaryWorksheet.Cell("A1").Style.Font.FontSize = 16;
                    summaryWorksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add report details
                    summaryWorksheet.Cell("A3").Value = "Period:";
                    summaryWorksheet.Cell("B3").Value = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";

                    if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                    {
                        summaryWorksheet.Cell("A4").Value = "Vehicle Type:";
                        summaryWorksheet.Cell("B4").Value = vehicleType;
                    }

                    // Add summary
                    summaryWorksheet.Cell("A6").Value = "SUMMARY";
                    summaryWorksheet.Cell("A6").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A7").Value = "Total Transactions:";
                    summaryWorksheet.Cell("B7").Value = transactions.Count;

                    summaryWorksheet.Cell("A8").Value = "New Subscriptions:";
                    summaryWorksheet.Cell("B8").Value = transactions.Count(t => t.Type == "MONTHLY_SUBSCRIPTION");

                    summaryWorksheet.Cell("A9").Value = "Renewals:";
                    summaryWorksheet.Cell("B9").Value = transactions.Count(t => t.Type == "MONTHLY_RENEWAL");

                    summaryWorksheet.Cell("A10").Value = "Total Amount:";
                    summaryWorksheet.Cell("B10").Value = transactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("B10").Style.NumberFormat.Format = "#,##0";

                    // Add vehicle type breakdown
                    summaryWorksheet.Cell("A12").Value = "VEHICLE TYPE BREAKDOWN";
                    summaryWorksheet.Cell("A12").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A13").Value = "Vehicle Type";
                    summaryWorksheet.Cell("B13").Value = "Count";
                    summaryWorksheet.Cell("C13").Value = "Amount";

                    summaryWorksheet.Range("A13:C13").Style.Font.Bold = true;
                    summaryWorksheet.Range("A13:C13").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Car statistics
                    var carTransactions = transactions.Where(t => t.VehicleId.StartsWith("C")).ToList();
                    summaryWorksheet.Cell("A14").Value = "Car";
                    summaryWorksheet.Cell("B14").Value = carTransactions.Count;
                    summaryWorksheet.Cell("C14").Value = carTransactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("C14").Style.NumberFormat.Format = "#,##0";

                    // Motorcycle statistics
                    var motorcycleTransactions = transactions.Where(t => t.VehicleId.StartsWith("M")).ToList();
                    summaryWorksheet.Cell("A15").Value = "Motorcycle";
                    summaryWorksheet.Cell("B15").Value = motorcycleTransactions.Count;
                    summaryWorksheet.Cell("C15").Value = motorcycleTransactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("C15").Style.NumberFormat.Format = "#,##0";

                    // Add payment method breakdown
                    summaryWorksheet.Cell("A17").Value = "PAYMENT METHOD BREAKDOWN";
                    summaryWorksheet.Cell("A17").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A18").Value = "Payment Method";
                    summaryWorksheet.Cell("B18").Value = "Count";
                    summaryWorksheet.Cell("C18").Value = "Amount";

                    summaryWorksheet.Range("A18:C18").Style.Font.Bold = true;
                    summaryWorksheet.Range("A18:C18").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Cash statistics
                    var cashTransactions = transactions.Where(t => t.PaymentMethod == "CASH").ToList();
                    summaryWorksheet.Cell("A19").Value = "Cash";
                    summaryWorksheet.Cell("B19").Value = cashTransactions.Count;
                    summaryWorksheet.Cell("C19").Value = cashTransactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("C19").Style.NumberFormat.Format = "#,##0";

                    // Momo statistics
                    var momoTransactions = transactions.Where(t => t.PaymentMethod == "MOMO").ToList();
                    summaryWorksheet.Cell("A20").Value = "Momo";
                    summaryWorksheet.Cell("B20").Value = momoTransactions.Count;
                    summaryWorksheet.Cell("C20").Value = momoTransactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("C20").Style.NumberFormat.Format = "#,##0";

                    // Stripe statistics
                    var stripeTransactions = transactions.Where(t => t.PaymentMethod == "STRIPE").ToList();
                    summaryWorksheet.Cell("A21").Value = "Stripe";
                    summaryWorksheet.Cell("B21").Value = stripeTransactions.Count;
                    summaryWorksheet.Cell("C21").Value = stripeTransactions.Sum(t => t.Amount);
                    summaryWorksheet.Cell("C21").Style.NumberFormat.Format = "#,##0";

                    // Auto-fit columns
                    summaryWorksheet.Columns().AdjustToContents();

                    // Add transactions worksheet
                    var transactionsWorksheet = workbook.Worksheets.Add("Transactions");

                    // Add headers
                    transactionsWorksheet.Cell("A1").Value = "Transaction ID";
                    transactionsWorksheet.Cell("B1").Value = "Date";
                    transactionsWorksheet.Cell("C1").Value = "Vehicle ID";
                    transactionsWorksheet.Cell("D1").Value = "License Plate";
                    transactionsWorksheet.Cell("E1").Value = "Registrant";
                    transactionsWorksheet.Cell("F1").Value = "Vehicle Type";
                    transactionsWorksheet.Cell("G1").Value = "Transaction Type";
                    transactionsWorksheet.Cell("H1").Value = "Amount";
                    transactionsWorksheet.Cell("I1").Value = "Payment Method";
                    transactionsWorksheet.Cell("J1").Value = "Status";

                    // Style headers
                    var headerRange = transactionsWorksheet.Range("A1:J1");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    int row = 2;
                    foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                    {
                        transactionsWorksheet.Cell(row, 1).Value = transaction.TransactionId;
                        transactionsWorksheet.Cell(row, 2).Value = transaction.Timestamp;
                        transactionsWorksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                        transactionsWorksheet.Cell(row, 3).Value = transaction.VehicleId;

                        // Add vehicle details if available
                        if (vehicleDetails.ContainsKey(transaction.VehicleId))
                        {
                            var vehicle = vehicleDetails[transaction.VehicleId];
                            transactionsWorksheet.Cell(row, 4).Value = vehicle.LicensePlate;
                            transactionsWorksheet.Cell(row, 5).Value = vehicle.OwnerName;
                            transactionsWorksheet.Cell(row, 6).Value = transaction.VehicleId.StartsWith("C") ? "Car" : "Motorcycle";
                        }
                        else
                        {
                            transactionsWorksheet.Cell(row, 4).Value = "N/A";
                            transactionsWorksheet.Cell(row, 5).Value = "N/A";
                            transactionsWorksheet.Cell(row, 6).Value = transaction.VehicleId.StartsWith("C") ? "Car" : "Motorcycle";
                        }

                        transactionsWorksheet.Cell(row, 7).Value = transaction.Type == "MONTHLY_SUBSCRIPTION" ? "New Subscription" : "Renewal";
                        transactionsWorksheet.Cell(row, 8).Value = transaction.Amount;
                        transactionsWorksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                        transactionsWorksheet.Cell(row, 9).Value = transaction.PaymentMethod;
                        transactionsWorksheet.Cell(row, 10).Value = transaction.Status;
                        row++;
                    }

                    // Auto-fit columns
                    transactionsWorksheet.Columns().AdjustToContents();

                    // Save workbook
                    workbook.SaveAs(filePath);
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscriptions to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/monthly-subscription-revenue/excel")]
        public async Task<IActionResult> ExportMonthlySubscriptionRevenueToExcel([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var revenueByCategory = await _transactionService.GetMonthlySubscriptionRevenueAsync(start, end);
                var dailyRevenue = await _transactionService.GetDailyMonthlySubscriptionRevenueAsync(start, end);

                // Calculate total revenue
                decimal totalRevenue = revenueByCategory["TOTAL"];

                // Generate Excel file name
                string fileName = $"MonthlySubscriptionRevenue_{start:yyyyMMdd}_{end:yyyyMMdd}.xlsx";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create Excel workbook
                using (var workbook = new XLWorkbook())
                {
                    // Add summary worksheet
                    var summaryWorksheet = workbook.Worksheets.Add("Summary");

                    // Add title
                    summaryWorksheet.Cell("A1").Value = "SMART PARKING SYSTEM - MONTHLY SUBSCRIPTION REVENUE REPORT";
                    summaryWorksheet.Range("A1:F1").Merge();
                    summaryWorksheet.Cell("A1").Style.Font.Bold = true;
                    summaryWorksheet.Cell("A1").Style.Font.FontSize = 16;
                    summaryWorksheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Add report details
                    summaryWorksheet.Cell("A3").Value = "Period:";
                    summaryWorksheet.Cell("B3").Value = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";

                    // Add summary
                    summaryWorksheet.Cell("A5").Value = "SUMMARY";
                    summaryWorksheet.Cell("A5").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A6").Value = "Total Revenue:";
                    summaryWorksheet.Cell("B6").Value = totalRevenue;
                    summaryWorksheet.Cell("B6").Style.NumberFormat.Format = "#,##0";

                    // Add vehicle type breakdown
                    summaryWorksheet.Cell("A8").Value = "VEHICLE TYPE BREAKDOWN";
                    summaryWorksheet.Cell("A8").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A9").Value = "Vehicle Type";
                    summaryWorksheet.Cell("B9").Value = "Amount";
                    summaryWorksheet.Cell("C9").Value = "Percentage";

                    summaryWorksheet.Range("A9:C9").Style.Font.Bold = true;
                    summaryWorksheet.Range("A9:C9").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Car statistics
                    summaryWorksheet.Cell("A10").Value = "Car";
                    summaryWorksheet.Cell("B10").Value = revenueByCategory.ContainsKey("CAR") ? revenueByCategory["CAR"] : 0;
                    summaryWorksheet.Cell("B10").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C10").Value = totalRevenue > 0 ? (revenueByCategory.ContainsKey("CAR") ? revenueByCategory["CAR"] / totalRevenue : 0) : 0;
                    summaryWorksheet.Cell("C10").Style.NumberFormat.Format = "0.00%";

                    // Motorcycle statistics
                    summaryWorksheet.Cell("A11").Value = "Motorcycle";
                    summaryWorksheet.Cell("B11").Value = revenueByCategory.ContainsKey("MOTORCYCLE") ? revenueByCategory["MOTORCYCLE"] : 0;
                    summaryWorksheet.Cell("B11").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C11").Value = totalRevenue > 0 ? (revenueByCategory.ContainsKey("MOTORCYCLE") ? revenueByCategory["MOTORCYCLE"] / totalRevenue : 0) : 0;
                    summaryWorksheet.Cell("C11").Style.NumberFormat.Format = "0.00%";

                    // Add payment method breakdown
                    summaryWorksheet.Cell("A13").Value = "PAYMENT METHOD BREAKDOWN";
                    summaryWorksheet.Cell("A13").Style.Font.Bold = true;

                    summaryWorksheet.Cell("A14").Value = "Payment Method";
                    summaryWorksheet.Cell("B14").Value = "Amount";
                    summaryWorksheet.Cell("C14").Value = "Percentage";

                    summaryWorksheet.Range("A14:C14").Style.Font.Bold = true;
                    summaryWorksheet.Range("A14:C14").Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Cash statistics
                    summaryWorksheet.Cell("A15").Value = "Cash";
                    summaryWorksheet.Cell("B15").Value = revenueByCategory.ContainsKey("CASH") ? revenueByCategory["CASH"] : 0;
                    summaryWorksheet.Cell("B15").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C15").Value = totalRevenue > 0 ? (revenueByCategory.ContainsKey("CASH") ? revenueByCategory["CASH"] / totalRevenue : 0) : 0;
                    summaryWorksheet.Cell("C15").Style.NumberFormat.Format = "0.00%";

                    // Momo statistics
                    summaryWorksheet.Cell("A16").Value = "Momo";
                    summaryWorksheet.Cell("B16").Value = revenueByCategory.ContainsKey("MOMO") ? revenueByCategory["MOMO"] : 0;
                    summaryWorksheet.Cell("B16").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C16").Value = totalRevenue > 0 ? (revenueByCategory.ContainsKey("MOMO") ? revenueByCategory["MOMO"] / totalRevenue : 0) : 0;
                    summaryWorksheet.Cell("C16").Style.NumberFormat.Format = "0.00%";

                    // Stripe statistics
                    summaryWorksheet.Cell("A17").Value = "Stripe";
                    summaryWorksheet.Cell("B17").Value = revenueByCategory.ContainsKey("STRIPE") ? revenueByCategory["STRIPE"] : 0;
                    summaryWorksheet.Cell("B17").Style.NumberFormat.Format = "#,##0";
                    summaryWorksheet.Cell("C17").Value = totalRevenue > 0 ? (revenueByCategory.ContainsKey("STRIPE") ? revenueByCategory["STRIPE"] / totalRevenue : 0) : 0;
                    summaryWorksheet.Cell("C17").Style.NumberFormat.Format = "0.00%";

                    // Auto-fit columns
                    summaryWorksheet.Columns().AdjustToContents();

                    // Add daily revenue worksheet
                    var dailyRevenueSheet = workbook.Worksheets.Add("Daily Revenue");

                    // Add headers
                    dailyRevenueSheet.Cell("A1").Value = "Date";
                    dailyRevenueSheet.Cell("B1").Value = "Total Revenue";
                    dailyRevenueSheet.Cell("C1").Value = "New Subscription";
                    dailyRevenueSheet.Cell("D1").Value = "Renewal";
                    dailyRevenueSheet.Cell("E1").Value = "Car";
                    dailyRevenueSheet.Cell("F1").Value = "Motorcycle";
                    dailyRevenueSheet.Cell("G1").Value = "Cash";
                    dailyRevenueSheet.Cell("H1").Value = "Momo";
                    dailyRevenueSheet.Cell("I1").Value = "Stripe";

                    // Style headers
                    var headerRange = dailyRevenueSheet.Range("A1:I1");
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // Add data
                    int row = 2;
                    decimal totalDailyRev = 0;
                    decimal totalNewSub = 0;
                    decimal totalRen = 0;
                    decimal totalCarRev = 0;
                    decimal totalMotorcycleRev = 0;
                    decimal totalCashRev = 0;
                    decimal totalMomoRev = 0;
                    decimal totalStripeRev = 0;

                    foreach (var day in dailyRevenue)
                    {
                        var dayData = day as dynamic;

                        dailyRevenueSheet.Cell(row, 1).Value = dayData.Date;
                        dailyRevenueSheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";

                        dailyRevenueSheet.Cell(row, 2).Value = dayData.Total;
                        dailyRevenueSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                        totalDailyRev += dayData.Total;

                        dailyRevenueSheet.Cell(row, 3).Value = dayData.NewSubscription;
                        dailyRevenueSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                        totalNewSub += dayData.NewSubscription;

                        dailyRevenueSheet.Cell(row, 4).Value = dayData.Renewal;
                        dailyRevenueSheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                        totalRen += dayData.Renewal;

                        dailyRevenueSheet.Cell(row, 5).Value = dayData.Car;
                        dailyRevenueSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                        totalCarRev += dayData.Car;

                        dailyRevenueSheet.Cell(row, 6).Value = dayData.Motorcycle;
                        dailyRevenueSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                        totalMotorcycleRev += dayData.Motorcycle;

                        dailyRevenueSheet.Cell(row, 7).Value = dayData.Cash;
                        dailyRevenueSheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                        totalCashRev += dayData.Cash;

                        dailyRevenueSheet.Cell(row, 8).Value = dayData.Momo;
                        dailyRevenueSheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                        totalMomoRev += dayData.Momo;

                        dailyRevenueSheet.Cell(row, 9).Value = dayData.Stripe;
                        dailyRevenueSheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                        totalStripeRev += dayData.Stripe;

                        row++;
                    }

                    // Add total row
                    dailyRevenueSheet.Cell(row, 1).Value = "TOTAL";
                    dailyRevenueSheet.Cell(row, 1).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 2).Value = totalDailyRev;
                    dailyRevenueSheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 2).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 3).Value = totalNewSub;
                    dailyRevenueSheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 3).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 4).Value = totalRen;
                    dailyRevenueSheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 4).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 5).Value = totalCarRev;
                    dailyRevenueSheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 5).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 6).Value = totalMotorcycleRev;
                    dailyRevenueSheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 6).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 7).Value = totalCashRev;
                    dailyRevenueSheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 7).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 8).Value = totalMomoRev;
                    dailyRevenueSheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 8).Style.Font.Bold = true;

                    dailyRevenueSheet.Cell(row, 9).Value = totalStripeRev;
                    dailyRevenueSheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
                    dailyRevenueSheet.Cell(row, 9).Style.Font.Bold = true;

                    // Auto-fit columns
                    dailyRevenueSheet.Columns().AdjustToContents();

                    // Save workbook
                    workbook.SaveAs(filePath);
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscription revenue to Excel");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private void AddTableRow(Table table, string label, string value, PdfFont labelFont, PdfFont valueFont)
        {
            table.AddCell(new Cell().Add(new Paragraph(label).SetFont(labelFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFont(valueFont)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        }

        private string FormatCurrency(decimal amount)
        {
            return amount.ToString("N0", CultureInfo.InvariantCulture);
        }

        [HttpGet("export/monthly-subscriptions/csv")]
        public async Task<IActionResult> ExportMonthlySubscriptionsToCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] string? vehicleType)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get monthly subscription transactions
                var transactions = await _transactionService.GetMonthlySubscriptionTransactionsAsync(start, end);

                // Filter by vehicle type if provided
                if (!string.IsNullOrEmpty(vehicleType) && vehicleType.ToUpper() != "ALL")
                {
                    if (vehicleType.ToUpper() == "CAR")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("C")).ToList();
                    }
                    else if (vehicleType.ToUpper() == "MOTORCYCLE")
                    {
                        transactions = transactions.Where(t => t.VehicleId.StartsWith("M")).ToList();
                    }
                }

                // Generate CSV file name
                string fileName = $"MonthlySubscriptions_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write header
                    writer.WriteLine("Transaction ID,Date,Vehicle ID,Transaction Type,Amount,Payment Method,Status");

                    // Write data
                    foreach (var transaction in transactions.OrderByDescending(t => t.Timestamp))
                    {
                        writer.WriteLine(
                            $"\"{transaction.TransactionId}\"," +
                            $"\"{transaction.Timestamp:dd/MM/yyyy HH:mm:ss}\"," +
                            $"\"{transaction.VehicleId}\"," +
                            $"\"{(transaction.Type == "MONTHLY_SUBSCRIPTION" ? "New Subscription" : "Renewal")}\"," +
                            $"\"{transaction.Amount}\"," +
                            $"\"{transaction.PaymentMethod}\"," +
                            $"\"{transaction.Status}\""
                        );
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscriptions to CSV");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("export/monthly-subscription-revenue/csv")]
        public async Task<IActionResult> ExportMonthlySubscriptionRevenueToCsv([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);

                // Get revenue data
                var dailyRevenue = await _transactionService.GetDailyMonthlySubscriptionRevenueAsync(start, end);

                // Generate CSV file name
                string fileName = $"MonthlySubscriptionRevenue_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
                string filePath = Path.Combine(_reportsDirectory, fileName);

                // Create CSV file
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write header
                    writer.WriteLine("Date,Total Revenue,New Subscription,Renewal,Car,Motorcycle,Cash,MoMo,Stripe");

                    // Write data
                    foreach (var day in dailyRevenue)
                    {
                        var dayData = day as dynamic;
                        writer.WriteLine(
                            $"\"{dayData.Date:dd/MM/yyyy}\"," +
                            $"\"{dayData.Total}\"," +
                            $"\"{dayData.NewSubscription}\"," +
                            $"\"{dayData.Renewal}\"," +
                            $"\"{dayData.Car}\"," +
                            $"\"{dayData.Motorcycle}\"," +
                            $"\"{dayData.Cash}\"," +
                            $"\"{dayData.Momo}\"," +
                            $"\"{dayData.Stripe}\""
                        );
                    }
                }

                // Return file
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting monthly subscription revenue to CSV");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
