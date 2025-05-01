using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SmartParking.Core.Models
{
    public class Vehicle
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("vehicleId")]
        public string VehicleId { get; set; }  // M001, C001

        [BsonElement("licensePlate")]
        public string LicensePlate { get; set; }

        [BsonElement("vehicleType")]
        public string VehicleType { get; set; }  // "CAR" or "MOTORBIKE"

        [BsonElement("status")]
        public string Status { get; set; }  // "PARKED" or "LEFT"

        [BsonElement("entryTime")]
        public DateTime EntryTime { get; set; }

        [BsonElement("exitTime")]
        public DateTime? ExitTime { get; set; }

        [BsonElement("slotId")]
        public string SlotId { get; set; }  // Reference to ParkingSlot

        [BsonElement("isMonthlyRegistered")]
        public bool IsMonthlyRegistered { get; set; } = false;

        [BsonElement("createdAt")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
