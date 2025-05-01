using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SmartParking.Core.Models
{
    public class MonthlyVehicle
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("vehicleId")]
        public string VehicleId { get; set; }  // Special ID for monthly vehicles (e.g., MM001, MC001)

        [BsonElement("licensePlate")]
        public string LicensePlate { get; set; }

        [BsonElement("vehicleType")]
        public string VehicleType { get; set; }  // "CAR" or "MOTORCYCLE"

        [BsonElement("customerName")]
        public string CustomerName { get; set; }

        [BsonElement("customerPhone")]
        public string CustomerPhone { get; set; }

        [BsonElement("customerEmail")]
        public string CustomerEmail { get; set; }

        [BsonElement("startDate")]
        public DateTime StartDate { get; set; }

        [BsonElement("endDate")]
        public DateTime EndDate { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }  // "VALID", "EXPIRED", "CANCELLED"

        [BsonElement("registrationDate")]
        public DateTime RegistrationDate { get; set; }

        [BsonElement("lastRenewalDate")]
        public DateTime? LastRenewalDate { get; set; }

        [BsonElement("packageDuration")]
        public int PackageDuration { get; set; }  // Duration in months

        [BsonElement("packageAmount")]
        public decimal PackageAmount { get; set; }  // Amount paid for the package

        [BsonElement("discountPercentage")]
        public int DiscountPercentage { get; set; }  // Discount percentage applied

        [BsonElement("fixedSlotId")]
        public string FixedSlotId { get; set; }  // Fixed parking slot assigned to this monthly vehicle

        [BsonElement("subscriptionId")]
        public string? SubscriptionId { get; set; }  // Optional subscription ID for recurring payments

        [BsonElement("createdAt")]
        public DateTime? CreatedAt { get; set; }  // Optional creation timestamp

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }  // Optional update timestamp
    }
}
