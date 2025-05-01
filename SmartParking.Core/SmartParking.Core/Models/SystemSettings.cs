using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace SmartParking.Core.Models
{
    public class SystemSettings : BaseModel
    {
        [BsonElement("settingKey")]
        public string SettingKey { get; set; }

        [BsonElement("settingValue")]
        public string SettingValue { get; set; }

        [BsonElement("settingType")]
        public string SettingType { get; set; } // "decimal", "int", "string", "json"

        [BsonElement("displayName")]
        public string DisplayName { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("category")]
        public string Category { get; set; } // "ParkingFees", "Discounts", "General"

        [BsonElement("sortOrder")]
        public int SortOrder { get; set; }
    }

    public class ParkingFeeSettings
    {
        public decimal CasualMotorbikeFee { get; set; }
        public decimal CasualCarFee { get; set; }
        public decimal MonthlyMotorbikeFee { get; set; }
        public decimal MonthlyCarFee { get; set; }
    }

    public class ParkingSpaceSettings
    {
        public int MotorcycleSlots { get; set; }
        public int CarSlots { get; set; }
        public List<ParkingZone> Zones { get; set; } = new List<ParkingZone>();
    }

    public class ParkingZone
    {
        public string ZoneId { get; set; }
        public string Name { get; set; }
        public int MotorcycleSlots { get; set; }
        public int CarSlots { get; set; }
    }

    public class DiscountSettings
    {
        public List<DiscountTier> DiscountTiers { get; set; } = new List<DiscountTier>();
    }

    public class DiscountTier
    {
        public int MinMonths { get; set; }
        public int MaxMonths { get; set; }
        public int DiscountPercentage { get; set; }
    }
}
