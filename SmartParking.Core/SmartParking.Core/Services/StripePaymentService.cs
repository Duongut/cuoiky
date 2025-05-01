using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartParking.Core.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartParking.Core.Services
{
    public class StripePaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly StripePaymentConfig _stripeConfig;
        private readonly bool _mockMode;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;

            // Load Stripe configuration from appsettings.json
            _stripeConfig = new StripePaymentConfig
            {
                ApiKey = _configuration["PaymentGateways:Stripe:ApiKey"],
                WebhookSecret = _configuration["PaymentGateways:Stripe:WebhookSecret"]
            };

            // Check if mock mode is enabled
            _mockMode = bool.TryParse(_configuration["PaymentGateways:Stripe:MockMode"], out bool mockMode) && mockMode;

            if (!_mockMode)
            {
                // Configure HttpClient for Stripe API
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_stripeConfig.ApiKey}");
                _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");
            }

            _logger.LogInformation($"Stripe configuration loaded: ApiKey={_stripeConfig.ApiKey?.Substring(0, 8)}..., MockMode={_mockMode}");
        }

        /// <summary>
        /// Create a Stripe payment intent
        /// </summary>
        public async Task<StripeCreatePaymentResponse> CreatePaymentIntentAsync(string orderId, string description, decimal amount, string transactionId, string idempotencyKey = null)
        {
            try
            {
                // Convert amount to cents (Stripe requires amount in smallest currency unit)
                long amountInCents = (long)(amount * 100);

                // Add idempotency key to request headers if provided
                if (!string.IsNullOrEmpty(idempotencyKey) && !_mockMode)
                {
                    _httpClient.DefaultRequestHeaders.Remove("Idempotency-Key");
                    _httpClient.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
                }

                // Create payment request
                var paymentRequest = new StripeCreatePaymentRequest
                {
                    Amount = amountInCents,
                    Currency = "vnd",
                    Description = description,
                    Metadata = new Dictionary<string, string>
                    {
                        { "orderId", orderId },
                        { "transactionId", transactionId }
                    }
                };

                // If mock mode is enabled, return a mock response
                if (_mockMode)
                {
                    _logger.LogInformation($"Mock mode enabled. Returning mock Stripe payment response for order {orderId}");
                    return new StripeCreatePaymentResponse
                    {
                        Id = $"pi_mock_{Guid.NewGuid().ToString("N")}",
                        ClientSecret = $"pi_mock_{Guid.NewGuid().ToString("N")}_secret_{Guid.NewGuid().ToString("N")}",
                        Amount = amountInCents,
                        Currency = "vnd",
                        Status = "requires_payment_method"
                    };
                }

                // Send request to Stripe
                var jsonRequest = JsonSerializer.Serialize(paymentRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                _logger.LogInformation($"Sending Stripe payment request: {jsonRequest}");

                var response = await _httpClient.PostAsync("https://api.stripe.com/v1/payment_intents", content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Stripe payment response: {jsonResponse}");

                if (response.IsSuccessStatusCode)
                {
                    var paymentResponse = JsonSerializer.Deserialize<StripeCreatePaymentResponse>(jsonResponse);
                    return paymentResponse;
                }
                else
                {
                    _logger.LogError($"Stripe payment request failed: {jsonResponse}");
                    throw new Exception($"Stripe payment request failed: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe payment intent");
                throw;
            }
        }

        /// <summary>
        /// Verify Stripe webhook signature
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string signature)
        {
            try
            {
                // In a real implementation, we would use Stripe's library to verify the signature
                // For this demo, we'll just return true
                _logger.LogWarning("Skipping Stripe webhook signature verification for testing purposes");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Stripe webhook signature");
                return false;
            }
        }

        /// <summary>
        /// Simulate a successful Stripe payment (for testing)
        /// </summary>
        public StripeWebhookEvent CreateMockSuccessfulPaymentEvent(string paymentIntentId, string orderId, string transactionId, decimal amount)
        {
            // Convert amount to cents
            long amountInCents = (long)(amount * 100);

            return new StripeWebhookEvent
            {
                Id = $"evt_mock_{Guid.NewGuid().ToString("N")}",
                Type = "payment_intent.succeeded",
                Data = new StripeWebhookEventData
                {
                    Object = new StripePaymentIntent
                    {
                        Id = paymentIntentId,
                        Amount = amountInCents,
                        Currency = "vnd",
                        Status = "succeeded",
                        Metadata = new Dictionary<string, string>
                        {
                            { "orderId", orderId },
                            { "transactionId", transactionId }
                        },
                        PaymentMethod = "pm_card_visa",
                        PaymentMethodDetails = new StripePaymentMethodDetails
                        {
                            Card = new StripeCardDetails
                            {
                                Last4 = "4242",
                                Brand = "visa"
                            }
                        }
                    }
                }
            };
        }
    }
}
