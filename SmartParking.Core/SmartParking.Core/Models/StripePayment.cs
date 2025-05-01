using System.Text.Json.Serialization;

namespace SmartParking.Core.Models
{
    // Request models
    public class StripeCreatePaymentRequest
    {
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "vnd";

        [JsonPropertyName("payment_method_types")]
        public string[] PaymentMethodTypes { get; set; } = new[] { "card" };

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }

    // Response models
    public class StripeCreatePaymentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    // Webhook notification model
    public class StripeWebhookEvent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public StripeWebhookEventData Data { get; set; }
    }

    public class StripeWebhookEventData
    {
        [JsonPropertyName("object")]
        public StripePaymentIntent Object { get; set; }
    }

    public class StripePaymentIntent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; }

        [JsonPropertyName("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonPropertyName("payment_method_details")]
        public StripePaymentMethodDetails PaymentMethodDetails { get; set; }
    }

    public class StripePaymentMethodDetails
    {
        [JsonPropertyName("card")]
        public StripeCardDetails Card { get; set; }
    }

    public class StripeCardDetails
    {
        [JsonPropertyName("last4")]
        public string Last4 { get; set; }

        [JsonPropertyName("brand")]
        public string Brand { get; set; }
    }

    // Configuration model
    public class StripePaymentConfig
    {
        public string ApiKey { get; set; }
        public string WebhookSecret { get; set; }
    }
}
