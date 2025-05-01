using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace SmartParking.Core.Models
{
    public class Transaction : BaseModel
    {
        [BsonElement("transactionId")]
        [Required(ErrorMessage = "Transaction ID is required")]
        public string TransactionId { get; set; }

        [BsonElement("idempotencyKey")]
        public string IdempotencyKey { get; set; } = Guid.NewGuid().ToString();

        [BsonElement("vehicleId")]
        [Required(ErrorMessage = "Vehicle ID is required")]
        public string VehicleId { get; set; }

        [BsonElement("amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be greater than or equal to 0")]
        public decimal Amount { get; set; }

        [BsonElement("type")]
        [Required(ErrorMessage = "Transaction type is required")]
        public string Type { get; set; }  // "PARKING_FEE", "MONTHLY_SUBSCRIPTION", etc.

        [BsonElement("paymentMethod")]
        [Required(ErrorMessage = "Payment method is required")]
        public string PaymentMethod { get; set; }  // "CASH", "MOMO", "STRIPE"

        [BsonElement("status")]
        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; }  // "PENDING", "COMPLETED", "FAILED", "TIMEOUT", "REFUNDED"

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [BsonElement("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }

        [BsonElement("description")]
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [BsonElement("paymentDetails")]
        public PaymentDetails PaymentDetails { get; set; }

        [BsonElement("invoiceUrl")]
        public string? InvoiceUrl { get; set; }

        [BsonElement("retryCount")]
        public int RetryCount { get; set; } = 0;

        [BsonElement("lastRetryAt")]
        public DateTime? LastRetryAt { get; set; }
    }

    public class PaymentDetails
    {
        [BsonElement("transactionId")]
        public string? TransactionId { get; set; }

        [BsonElement("cashierName")]
        public string? CashierName { get; set; }

        [BsonElement("paymentTime")]
        public DateTime? PaymentTime { get; set; }

        [BsonElement("momoTransactionId")]
        public string? MomoTransactionId { get; set; }

        [BsonElement("stripePaymentIntentId")]
        public string? StripePaymentIntentId { get; set; }

        [BsonElement("cardLast4")]
        public string? CardLast4 { get; set; }

        [BsonElement("transactionReference")]
        public string? TransactionReference { get; set; }

        [BsonElement("paymentProvider")]
        public string? PaymentProvider { get; set; }

        [BsonElement("additionalInfo")]
        public string? AdditionalInfo { get; set; }

        [BsonElement("paymentUrl")]
        public string? PaymentUrl { get; set; }

        [BsonElement("paymentResponse")]
        public string? PaymentResponse { get; set; }
    }
}
