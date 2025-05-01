using MongoDB.Driver;
using MongoDB.Bson;
using SmartParking.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartParking.Core.Data
{
    public class FixMonthlyVehicleSchema
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<FixMonthlyVehicleSchema> _logger;

        public FixMonthlyVehicleSchema(IConfiguration configuration, ILogger<FixMonthlyVehicleSchema> logger)
        {
            var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
            _database = client.GetDatabase(configuration.GetSection("DatabaseSettings")["DatabaseName"]);
            _logger = logger;
        }

        public async Task FixMonthlyVehiclesSchema()
        {
            _logger.LogInformation("Starting MongoDB schema fix for MonthlyVehicles collection...");

            try
            {
                // Check if the collection exists
                if (!await CollectionExistsAsync("MonthlyVehicles"))
                {
                    _logger.LogInformation("MonthlyVehicles collection does not exist. Creating new collection.");
                    await _database.CreateCollectionAsync("MonthlyVehicles");
                    _logger.LogInformation("Created new MonthlyVehicles collection");
                    return;
                }

                // Create a new collection with the correct schema
                if (await CollectionExistsAsync("MonthlyVehicles_New"))
                {
                    await _database.DropCollectionAsync("MonthlyVehicles_New");
                    _logger.LogInformation("Dropped existing MonthlyVehicles_New collection");
                }

                await _database.CreateCollectionAsync("MonthlyVehicles_New");
                _logger.LogInformation("Created new MonthlyVehicles_New collection");

                // Get raw monthly vehicles from the current collection to avoid deserialization issues
                var rawCollection = _database.GetCollection<BsonDocument>("MonthlyVehicles");
                var rawVehicles = await rawCollection.Find(new BsonDocument()).ToListAsync();
                _logger.LogInformation($"Found {rawVehicles.Count} monthly vehicles in the database");

                if (rawVehicles.Count == 0)
                {
                    _logger.LogInformation("No monthly vehicles to migrate.");
                    return;
                }

                var newCollection = _database.GetCollection<BsonDocument>("MonthlyVehicles_New");

                // Migrate each monthly vehicle to the new collection with the updated schema
                foreach (var rawVehicle in rawVehicles)
                {
                    // Handle the 'ownerInfo' field that's causing the error
                    if (rawVehicle.Contains("ownerInfo") && rawVehicle["ownerInfo"].IsBsonDocument)
                    {
                        var ownerInfo = rawVehicle["ownerInfo"].AsBsonDocument;

                        // Extract fields from ownerInfo and add them directly to the document
                        if (ownerInfo.Contains("name"))
                        {
                            rawVehicle["customerName"] = ownerInfo["name"];
                        }

                        if (ownerInfo.Contains("phone"))
                        {
                            rawVehicle["customerPhone"] = ownerInfo["phone"];
                        }

                        if (ownerInfo.Contains("email"))
                        {
                            rawVehicle["customerEmail"] = ownerInfo["email"];
                        }

                        // Remove the ownerInfo field
                        rawVehicle.Remove("ownerInfo");
                    }

                    // Handle the 'subscriptionId' field that's causing the error
                    if (rawVehicle.Contains("subscriptionId"))
                    {
                        // Remove the subscriptionId field as it's not in the model
                        rawVehicle.Remove("subscriptionId");
                    }

                    // Handle the 'createdAt' field that's causing the error
                    if (rawVehicle.Contains("createdAt"))
                    {
                        // Remove the createdAt field as it's not in the model
                        rawVehicle.Remove("createdAt");
                    }

                    // Handle the 'updatedAt' field that's causing the error
                    if (rawVehicle.Contains("updatedAt"))
                    {
                        // Remove the updatedAt field as it's not in the model
                        rawVehicle.Remove("updatedAt");
                    }

                    // Add required fields if they don't exist
                    if (!rawVehicle.Contains("customerName"))
                    {
                        rawVehicle["customerName"] = "Unknown";
                    }

                    if (!rawVehicle.Contains("customerPhone"))
                    {
                        rawVehicle["customerPhone"] = "Unknown";
                    }

                    if (!rawVehicle.Contains("customerEmail"))
                    {
                        rawVehicle["customerEmail"] = "unknown@example.com";
                    }

                    // Add other required fields with default values if they don't exist
                    if (!rawVehicle.Contains("status"))
                    {
                        rawVehicle["status"] = "VALID";
                    }

                    if (!rawVehicle.Contains("registrationDate"))
                    {
                        rawVehicle["registrationDate"] = DateTime.UtcNow;
                    }

                    if (!rawVehicle.Contains("startDate"))
                    {
                        rawVehicle["startDate"] = DateTime.UtcNow;
                    }

                    if (!rawVehicle.Contains("endDate"))
                    {
                        // Default to 1 month from start date
                        var startDate = rawVehicle.Contains("startDate") ?
                            rawVehicle["startDate"].ToUniversalTime() : DateTime.UtcNow;
                        rawVehicle["endDate"] = startDate.AddMonths(1);
                    }

                    if (!rawVehicle.Contains("packageDuration"))
                    {
                        rawVehicle["packageDuration"] = 1;
                    }

                    if (!rawVehicle.Contains("packageAmount"))
                    {
                        // Default amount based on vehicle type
                        decimal amount = 100000; // Default for motorcycle
                        if (rawVehicle.Contains("vehicleType") &&
                            rawVehicle["vehicleType"].AsString.ToUpper() == "CAR")
                        {
                            amount = 300000;
                        }
                        rawVehicle["packageAmount"] = amount;
                    }

                    if (!rawVehicle.Contains("discountPercentage"))
                    {
                        rawVehicle["discountPercentage"] = 0;
                    }

                    await newCollection.InsertOneAsync(rawVehicle);
                }

                _logger.LogInformation($"Migrated {rawVehicles.Count} monthly vehicles to the new collection");

                // Rename collections to swap the old and new
                try
                {
                    // Drop old backup if it exists
                    if (await CollectionExistsAsync("MonthlyVehicles_Old"))
                    {
                        await _database.DropCollectionAsync("MonthlyVehicles_Old");
                        _logger.LogInformation("Dropped existing MonthlyVehicles_Old collection");
                    }

                    await _database.RenameCollectionAsync("MonthlyVehicles", "MonthlyVehicles_Old");
                    _logger.LogInformation("Renamed MonthlyVehicles to MonthlyVehicles_Old");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error renaming MonthlyVehicles to MonthlyVehicles_Old: {ex.Message}");
                    // If we can't rename, drop the original collection
                    await _database.DropCollectionAsync("MonthlyVehicles");
                    _logger.LogInformation("Dropped original MonthlyVehicles collection");
                }

                try
                {
                    await _database.RenameCollectionAsync("MonthlyVehicles_New", "MonthlyVehicles");
                    _logger.LogInformation("Renamed MonthlyVehicles_New to MonthlyVehicles");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error renaming MonthlyVehicles_New to MonthlyVehicles: {ex.Message}");
                    // If we can't rename, create a new collection and copy the data
                    var tempCollection = _database.GetCollection<BsonDocument>("MonthlyVehicles_New");
                    var vehicles = await tempCollection.Find(new BsonDocument()).ToListAsync();

                    await _database.CreateCollectionAsync("MonthlyVehicles");
                    var finalCollection = _database.GetCollection<BsonDocument>("MonthlyVehicles");

                    if (vehicles.Count > 0)
                    {
                        await finalCollection.InsertManyAsync(vehicles);
                    }
                    _logger.LogInformation("Created new MonthlyVehicles collection with migrated data");
                }

                _logger.LogInformation("MonthlyVehicles schema fix completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fixing MonthlyVehicles schema: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task<bool> CollectionExistsAsync(string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await _database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }
    }
}
