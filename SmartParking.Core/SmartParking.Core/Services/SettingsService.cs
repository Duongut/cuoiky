using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SmartParking.Core.Data;
using SmartParking.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class SettingsService
    {
        private readonly MongoDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsService> _logger;
        private Dictionary<string, SystemSettings> _cachedSettings;
        private DateTime _cacheExpiration = DateTime.MinValue;

        public SettingsService(MongoDBContext context, IConfiguration configuration, ILogger<SettingsService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _cachedSettings = new Dictionary<string, SystemSettings>();
        }

        /// <summary>
        /// Initialize system settings if they don't exist
        /// </summary>
        public async Task InitializeSettingsAsync()
        {
            // Check if settings already exist
            var count = await _context.SystemSettings.CountDocumentsAsync(FilterDefinition<SystemSettings>.Empty);
            if (count > 0)
            {
                _logger.LogInformation("System settings already initialized");
                return;
            }

            _logger.LogInformation("Initializing system settings from configuration");

            var settings = new List<SystemSettings>();

            // Parking fees
            var feeConfig = _configuration.GetSection("ParkingFees");
            settings.Add(CreateSetting("CasualMotorbikeFee", feeConfig["CasualMotorbikeFee"] ?? "10000", "decimal", "Casual Motorbike Fee", "Fee for casual motorbike parking (VND)", "ParkingFees", 1));
            settings.Add(CreateSetting("CasualCarFee", feeConfig["CasualCarFee"] ?? "30000", "decimal", "Casual Car Fee", "Fee for casual car parking (VND)", "ParkingFees", 2));
            settings.Add(CreateSetting("MonthlyMotorbikeFee", feeConfig["MonthlyMotorbikeFee"] ?? "100000", "decimal", "Monthly Motorbike Fee", "Base fee for monthly motorbike parking (VND/month)", "ParkingFees", 3));
            settings.Add(CreateSetting("MonthlyCarFee", feeConfig["MonthlyCarFee"] ?? "300000", "decimal", "Monthly Car Fee", "Base fee for monthly car parking (VND/month)", "ParkingFees", 4));

            // Parking spaces
            var parkingConfig = _configuration.GetSection("ParkingSettings");
            var parkingSpaceSettings = new ParkingSpaceSettings
            {
                MotorcycleSlots = parkingConfig.GetValue<int>("MotorcycleSlots", 200),
                CarSlots = parkingConfig.GetValue<int>("CarSlots", 50),
                Zones = new List<ParkingZone>()
            };

            // Add zones if they exist in configuration
            var zonesConfig = parkingConfig.GetSection("Zones");
            if (zonesConfig.Exists())
            {
                foreach (var zoneConfig in zonesConfig.GetChildren())
                {
                    parkingSpaceSettings.Zones.Add(new ParkingZone
                    {
                        ZoneId = zoneConfig["ZoneId"] ?? $"Zone{parkingSpaceSettings.Zones.Count + 1}",
                        Name = zoneConfig["Name"] ?? $"Zone {parkingSpaceSettings.Zones.Count + 1}",
                        MotorcycleSlots = zoneConfig.GetValue<int>("MotorcycleSlots", 0),
                        CarSlots = zoneConfig.GetValue<int>("CarSlots", 0)
                    });
                }
            }

            settings.Add(CreateSetting("ParkingSpaces", JsonSerializer.Serialize(parkingSpaceSettings), "json", "Parking Spaces", "Configuration for parking spaces", "ParkingSpaces", 1));

            // Discount tiers
            var discountTiers = new List<DiscountTier>
            {
                new DiscountTier { MinMonths = 1, MaxMonths = 2, DiscountPercentage = 0 },
                new DiscountTier { MinMonths = 3, MaxMonths = 5, DiscountPercentage = 10 },
                new DiscountTier { MinMonths = 6, MaxMonths = 11, DiscountPercentage = 20 },
                new DiscountTier { MinMonths = 12, MaxMonths = int.MaxValue, DiscountPercentage = 40 }
            };

            var discountSettings = new DiscountSettings { DiscountTiers = discountTiers };
            settings.Add(CreateSetting("DiscountTiers", JsonSerializer.Serialize(discountSettings), "json", "Discount Tiers", "Discount tiers for monthly parking", "Discounts", 1));

            // Insert all settings
            await _context.SystemSettings.InsertManyAsync(settings);
            _logger.LogInformation($"Initialized {settings.Count} system settings");
        }

        private SystemSettings CreateSetting(string key, string value, string type, string displayName, string description, string category, int sortOrder)
        {
            return new SystemSettings
            {
                SettingKey = key,
                SettingValue = value,
                SettingType = type,
                DisplayName = displayName,
                Description = description,
                Category = category,
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get all system settings
        /// </summary>
        public async Task<List<SystemSettings>> GetAllSettingsAsync()
        {
            return await _context.SystemSettings.Find(FilterDefinition<SystemSettings>.Empty)
                .SortBy(s => s.Category)
                .ThenBy(s => s.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get settings by category
        /// </summary>
        public async Task<List<SystemSettings>> GetSettingsByCategoryAsync(string category)
        {
            return await _context.SystemSettings.Find(s => s.Category == category)
                .SortBy(s => s.SortOrder)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific setting by key
        /// </summary>
        public async Task<SystemSettings> GetSettingByKeyAsync(string key)
        {
            return await _context.SystemSettings.Find(s => s.SettingKey == key).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Update a setting
        /// </summary>
        public async Task<SystemSettings> UpdateSettingAsync(string key, string value)
        {
            var setting = await GetSettingByKeyAsync(key);
            if (setting == null)
            {
                throw new Exception($"Setting with key {key} not found");
            }

            // Validate value based on type
            switch (setting.SettingType)
            {
                case "decimal":
                    if (!decimal.TryParse(value, out _))
                    {
                        throw new Exception($"Invalid decimal value: {value}");
                    }
                    break;
                case "int":
                    if (!int.TryParse(value, out _))
                    {
                        throw new Exception($"Invalid integer value: {value}");
                    }
                    break;
                case "json":
                    try
                    {
                        JsonDocument.Parse(value);
                    }
                    catch (JsonException ex)
                    {
                        throw new Exception($"Invalid JSON value: {ex.Message}");
                    }
                    break;
            }

            // Update the setting
            var filter = Builders<SystemSettings>.Filter.Eq(s => s.SettingKey, key);
            var update = Builders<SystemSettings>.Update
                .Set(s => s.SettingValue, value)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _context.SystemSettings.UpdateOneAsync(filter, update);

            // Clear cache
            _cachedSettings.Clear();
            _cacheExpiration = DateTime.MinValue;

            // Return the updated setting
            return await GetSettingByKeyAsync(key);
        }

        /// <summary>
        /// Create or update a setting
        /// </summary>
        public async Task<SystemSettings> CreateOrUpdateSettingAsync(string key, string value, string type, string displayName, string description, string category, int sortOrder)
        {
            var setting = await GetSettingByKeyAsync(key);
            if (setting == null)
            {
                // Create new setting
                setting = CreateSetting(key, value, type, displayName, description, category, sortOrder);
                await _context.SystemSettings.InsertOneAsync(setting);
                _logger.LogInformation($"Created new setting with key {key}");

                // Clear cache
                _cachedSettings.Clear();
                _cacheExpiration = DateTime.MinValue;

                return setting;
            }
            else
            {
                // Update existing setting
                return await UpdateSettingAsync(key, value);
            }
        }

        /// <summary>
        /// Get parking fee settings
        /// </summary>
        public async Task<ParkingFeeSettings> GetParkingFeeSettingsAsync()
        {
            await RefreshCacheIfNeededAsync();

            return new ParkingFeeSettings
            {
                CasualMotorbikeFee = GetDecimalSetting("CasualMotorbikeFee", 10000),
                CasualCarFee = GetDecimalSetting("CasualCarFee", 30000),
                MonthlyMotorbikeFee = GetDecimalSetting("MonthlyMotorbikeFee", 100000),
                MonthlyCarFee = GetDecimalSetting("MonthlyCarFee", 300000)
            };
        }

        /// <summary>
        /// Get discount settings
        /// </summary>
        public async Task<DiscountSettings> GetDiscountSettingsAsync()
        {
            await RefreshCacheIfNeededAsync();

            if (_cachedSettings.TryGetValue("DiscountTiers", out var setting))
            {
                try
                {
                    return JsonSerializer.Deserialize<DiscountSettings>(setting.SettingValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing discount settings");
                }
            }

            // Return default discount settings if not found or error
            return new DiscountSettings
            {
                DiscountTiers = new List<DiscountTier>
                {
                    new DiscountTier { MinMonths = 1, MaxMonths = 2, DiscountPercentage = 0 },
                    new DiscountTier { MinMonths = 3, MaxMonths = 5, DiscountPercentage = 10 },
                    new DiscountTier { MinMonths = 6, MaxMonths = 11, DiscountPercentage = 20 },
                    new DiscountTier { MinMonths = 12, MaxMonths = int.MaxValue, DiscountPercentage = 40 }
                }
            };
        }

        /// <summary>
        /// Get discount percentage for a given duration
        /// </summary>
        public async Task<int> GetDiscountPercentageAsync(int durationMonths)
        {
            var discountSettings = await GetDiscountSettingsAsync();

            foreach (var tier in discountSettings.DiscountTiers)
            {
                if (durationMonths >= tier.MinMonths && durationMonths <= tier.MaxMonths)
                {
                    return tier.DiscountPercentage;
                }
            }

            return 0; // Default: no discount
        }

        /// <summary>
        /// Update parking fee settings
        /// </summary>
        public async Task UpdateParkingFeeSettingsAsync(ParkingFeeSettings settings)
        {
            await UpdateSettingAsync("CasualMotorbikeFee", settings.CasualMotorbikeFee.ToString());
            await UpdateSettingAsync("CasualCarFee", settings.CasualCarFee.ToString());
            await UpdateSettingAsync("MonthlyMotorbikeFee", settings.MonthlyMotorbikeFee.ToString());
            await UpdateSettingAsync("MonthlyCarFee", settings.MonthlyCarFee.ToString());
        }

        /// <summary>
        /// Update discount settings
        /// </summary>
        public async Task UpdateDiscountSettingsAsync(DiscountSettings settings)
        {
            // Validate discount tiers
            if (settings.DiscountTiers == null || settings.DiscountTiers.Count == 0)
            {
                throw new Exception("At least one discount tier is required");
            }

            // Sort tiers by min months
            settings.DiscountTiers.Sort((a, b) => a.MinMonths.CompareTo(b.MinMonths));

            // Validate tier ranges
            for (int i = 0; i < settings.DiscountTiers.Count - 1; i++)
            {
                if (settings.DiscountTiers[i].MaxMonths >= settings.DiscountTiers[i + 1].MinMonths)
                {
                    throw new Exception("Discount tier ranges must not overlap");
                }
            }

            // Update the setting
            await UpdateSettingAsync("DiscountTiers", JsonSerializer.Serialize(settings));
        }

        /// <summary>
        /// Get parking space settings
        /// </summary>
        public async Task<ParkingSpaceSettings> GetParkingSpaceSettingsAsync()
        {
            await RefreshCacheIfNeededAsync();

            if (_cachedSettings.TryGetValue("ParkingSpaces", out var setting))
            {
                try
                {
                    return JsonSerializer.Deserialize<ParkingSpaceSettings>(setting.SettingValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing parking space settings");
                }
            }

            // Return default parking space settings if not found or error
            var parkingConfig = _configuration.GetSection("ParkingSettings");
            return new ParkingSpaceSettings
            {
                MotorcycleSlots = parkingConfig.GetValue<int>("MotorcycleSlots", 200),
                CarSlots = parkingConfig.GetValue<int>("CarSlots", 50),
                Zones = new List<ParkingZone>()
            };
        }

        /// <summary>
        /// Update parking space settings
        /// </summary>
        public async Task UpdateParkingSpaceSettingsAsync(ParkingSpaceSettings settings)
        {
            // Validate settings
            if (settings.MotorcycleSlots < 0)
            {
                throw new Exception("Number of motorcycle slots cannot be negative");
            }

            if (settings.CarSlots < 0)
            {
                throw new Exception("Number of car slots cannot be negative");
            }

            // Validate zones if they exist
            if (settings.Zones != null)
            {
                foreach (var zone in settings.Zones)
                {
                    if (string.IsNullOrWhiteSpace(zone.ZoneId))
                    {
                        throw new Exception("Zone ID cannot be empty");
                    }

                    if (string.IsNullOrWhiteSpace(zone.Name))
                    {
                        throw new Exception("Zone name cannot be empty");
                    }

                    if (zone.MotorcycleSlots < 0)
                    {
                        throw new Exception($"Number of motorcycle slots in zone {zone.Name} cannot be negative");
                    }

                    if (zone.CarSlots < 0)
                    {
                        throw new Exception($"Number of car slots in zone {zone.Name} cannot be negative");
                    }
                }

                // Check for duplicate zone IDs
                var zoneIds = settings.Zones.Select(z => z.ZoneId).ToList();
                if (zoneIds.Count != zoneIds.Distinct().Count())
                {
                    throw new Exception("Zone IDs must be unique");
                }
            }

            // Create or update the setting
            await CreateOrUpdateSettingAsync(
                "ParkingSpaces",
                JsonSerializer.Serialize(settings),
                "json",
                "Parking Spaces",
                "Configuration for parking spaces",
                "ParkingSpaces",
                1);
        }

        /// <summary>
        /// Refresh the settings cache if needed
        /// </summary>
        private async Task RefreshCacheIfNeededAsync()
        {
            if (_cachedSettings.Count == 0 || DateTime.UtcNow > _cacheExpiration)
            {
                var settings = await GetAllSettingsAsync();
                var newCache = new Dictionary<string, SystemSettings>();

                foreach (var setting in settings)
                {
                    newCache[setting.SettingKey] = setting;
                }

                _cachedSettings = newCache;
                _cacheExpiration = DateTime.UtcNow.AddMinutes(5); // Cache for 5 minutes
            }
        }

        /// <summary>
        /// Get a decimal setting value from cache
        /// </summary>
        private decimal GetDecimalSetting(string key, decimal defaultValue)
        {
            if (_cachedSettings.TryGetValue(key, out var setting))
            {
                if (decimal.TryParse(setting.SettingValue, out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
    }
}
