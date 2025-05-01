using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SmartParking.Core.Data;
using SmartParking.Core.Models;

namespace SmartParking.Core
{
    public class DeleteM001Duplicates
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting DeleteM001Duplicates program...");

            try
            {
                // Create a MongoDB client
                var client = new MongoClient("mongodb://localhost:27017");
                var database = client.GetDatabase("SmartParkingDb");
                var vehiclesCollection = database.GetCollection<Vehicle>("Vehicles");

                // Find all vehicles with vehicleId M001
                var filter = Builders<Vehicle>.Filter.Eq(v => v.VehicleId, "M001");
                var vehicles = await vehiclesCollection.Find(filter).ToListAsync();

                Console.WriteLine($"Found {vehicles.Count} vehicles with vehicleId M001");

                if (vehicles.Count > 1)
                {
                    // Sort by CreatedAt (newest first)
                    var sortedVehicles = vehicles.OrderByDescending(v => v.CreatedAt ?? DateTime.MinValue).ToList();

                    // Keep the most recent one
                    var keepVehicle = sortedVehicles.First();
                    Console.WriteLine($"Keeping the most recent vehicle with _id: {keepVehicle.Id}");

                    // Delete the others
                    for (int i = 1; i < sortedVehicles.Count; i++)
                    {
                        var deleteFilter = Builders<Vehicle>.Filter.Eq(v => v.Id, sortedVehicles[i].Id);
                        var result = await vehiclesCollection.DeleteOneAsync(deleteFilter);
                        Console.WriteLine($"Deleted vehicle with _id: {sortedVehicles[i].Id}, DeleteResult: {result.DeletedCount}");
                    }

                    Console.WriteLine($"Deleted {sortedVehicles.Count - 1} duplicate vehicles");
                }
                else
                {
                    Console.WriteLine("No duplicates found, nothing to delete");
                }

                // Verify the result
                var remainingVehicles = await vehiclesCollection.Find(filter).ToListAsync();
                Console.WriteLine($"After cleanup: Found {remainingVehicles.Count} vehicles with vehicleId M001");

                // Now let's try to drop and recreate the index
                try
                {
                    await vehiclesCollection.Indexes.DropOneAsync("idx_vehicleId");
                    Console.WriteLine("Dropped idx_vehicleId index");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not drop idx_vehicleId index: {ex.Message}");
                }

                // Recreate the index
                var indexKeys = Builders<Vehicle>.IndexKeys.Ascending(v => v.VehicleId);
                var indexOptions = new CreateIndexOptions { Name = "idx_vehicleId", Unique = true };
                var indexModel = new CreateIndexModel<Vehicle>(indexKeys, indexOptions);
                
                await vehiclesCollection.Indexes.CreateOneAsync(indexModel);
                Console.WriteLine("Recreated idx_vehicleId index");

                Console.WriteLine("DeleteM001Duplicates program completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
