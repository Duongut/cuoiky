using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartParking.Core.Models;
using SmartParking.Core.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartParking.Core.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly ILogger<TransactionController> _logger;
        private readonly TransactionService _transactionService;

        public TransactionController(
            ILogger<TransactionController> logger,
            TransactionService transactionService)
        {
            _logger = logger;
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all transactions");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransactionById(string id)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    return NotFound(new { error = $"Transaction with ID {id} not found" });
                }
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction with ID {id}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetTransactionSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                _logger.LogInformation("Getting transaction summary");
                
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today;
                var end = endDate ?? DateTime.Now;
                
                _logger.LogInformation($"Date range: {start} to {end}");

                // Get transactions for the specified date range
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);
                var completedTransactions = transactions.Where(t => t.Status == "COMPLETED").ToList();
                
                _logger.LogInformation($"Found {completedTransactions.Count} completed transactions in the date range");

                // Calculate summary
                var totalAmount = completedTransactions.Sum(t => t.Amount);
                var cashAmount = completedTransactions.Where(t => t.PaymentMethod == "CASH").Sum(t => t.Amount);
                var momoAmount = completedTransactions.Where(t => t.PaymentMethod == "MOMO").Sum(t => t.Amount);
                var stripeAmount = completedTransactions.Where(t => t.PaymentMethod == "STRIPE").Sum(t => t.Amount);
                
                _logger.LogInformation($"Total amount: {totalAmount}");

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    totalAmount = totalAmount,
                    cashAmount = cashAmount,
                    momoAmount = momoAmount,
                    stripeAmount = stripeAmount,
                    transactionCount = completedTransactions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction summary");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("by-date-range")]
        public async Task<IActionResult> GetTransactionsByDateRange(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? paymentMethod)
        {
            try
            {
                // Set default date range if not provided
                var start = startDate ?? DateTime.Today.AddDays(-7);
                var end = endDate ?? DateTime.Now;

                // Get transactions
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(start, end);

                // Filter by payment method if provided
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod.ToUpper() != "ALL")
                {
                    transactions = transactions.Where(t => t.PaymentMethod.ToUpper() == paymentMethod.ToUpper()).ToList();
                }

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions by date range");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
