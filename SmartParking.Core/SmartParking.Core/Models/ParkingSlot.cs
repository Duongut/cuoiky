using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace SmartParking.Core.Models
{
    public class ParkingSlot
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("slotId")]
        public string SlotId { get; set; }  // Mã vị trí đỗ

        [BsonElement("type")]
        public string Type { get; set; }  // "CAR" or "MOTORBIKE"

        [BsonElement("status")]
        public string Status { get; set; }  // "AVAILABLE" (free for any vehicle), "OCCUPIED" (currently has a vehicle), or "RESERVED" (dedicated slot for monthly vehicles)

        [BsonElement("currentVehicleId")]
        public string CurrentVehicleId { get; set; }  // Reference to Vehicle

        [BsonElement("createdAt")]
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
