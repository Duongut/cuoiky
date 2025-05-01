using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartParking.Core.Data
{
    public class MongoDBCleanupUtility
    {
        private readonly MongoDBContext _context;
        private readonly ILogger<MongoDBCleanupUtility> _logger;

        public MongoDBCleanupUtility(MongoDBContext context, ILogger<MongoDBCleanupUtility> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Removes duplicate records from the Vehicles collection based on VehicleId
        /// </summary>
        public async Task CleanupDuplicateVehiclesAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of duplicate vehicles...");

                // Get all vehicles
                var vehicles = await _context.Vehicles.Find(_ => true).ToListAsync();

                // Group vehicles by VehicleId
                var groupedVehicles = vehicles.GroupBy(v => v.VehicleId)
                    .Where(g => g.Count() > 1)
                    .ToList();

                _logger.LogInformation($"Found {groupedVehicles.Count} vehicle IDs with duplicates");

                foreach (var group in groupedVehicles)
                {
                    _logger.LogInformation($"Processing duplicates for VehicleId: {group.Key}");

                    // Sort by CreatedAt to keep the most recent one
                    var sortedVehicles = group.OrderByDescending(v => v.CreatedAt ?? DateTime.MinValue)
                        .ToList();

                    // Keep the first one (most recent) and delete the rest
                    var keepVehicle = sortedVehicles.First();
                    var deleteVehicles = sortedVehicles.Skip(1).ToList();

                    _logger.LogInformation($"Keeping vehicle with Id: {keepVehicle.Id}, deleting {deleteVehicles.Count} duplicates");

                    foreach (var vehicle in deleteVehicles)
                    {
                        await _context.Vehicles.DeleteOneAsync(v => v.Id == vehicle.Id);
                        _logger.LogInformation($"Deleted duplicate vehicle with Id: {vehicle.Id}");
                    }
                }

                _logger.LogInformation("Completed cleanup of duplicate vehicles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicate vehicles");
                throw;
            }
        }

        /// <summary>
        /// Removes duplicate records from the Transactions collection based on TransactionId
        /// </summary>
        public async Task CleanupDuplicateTransactionsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of duplicate transactions...");

                // Get all transactions
                var transactions = await _context.Transactions.Find(_ => true).ToListAsync();

                // Group transactions by TransactionId
                var groupedTransactions = transactions.GroupBy(t => t.TransactionId)
                    .Where(g => g.Count() > 1 || string.IsNullOrEmpty(g.Key))
                    .ToList();

                _logger.LogInformation($"Found {groupedTransactions.Count} transaction IDs with duplicates or null values");

                // First, handle null TransactionIds
                var nullTransactionIdGroup = groupedTransactions.FirstOrDefault(g => string.IsNullOrEmpty(g.Key));
                if (nullTransactionIdGroup != null)
                {
                    _logger.LogInformation($"Processing {nullTransactionIdGroup.Count()} transactions with null TransactionId");

                    // For each transaction with null TransactionId, generate a new unique one
                    foreach (var transaction in nullTransactionIdGroup)
                    {
                        // Generate a new unique TransactionId
                        var guid = Guid.NewGuid().ToString();
                        var newTransactionId = $"TRX{DateTime.Now:yyyyMMddHHmmss}{guid.Substring(0, 4)}";

                        // Update the transaction with the new ID
                        var update = Builders<Models.Transaction>.Update
                            .Set(t => t.TransactionId, newTransactionId);

                        await _context.Transactions.UpdateOneAsync(t => t.Id == transaction.Id, update);
                        _logger.LogInformation($"Updated transaction {transaction.Id} with new TransactionId: {newTransactionId}");
                    }
                }

                // Now handle actual duplicates (excluding the null group we just fixed)
                foreach (var group in groupedTransactions.Where(g => !string.IsNullOrEmpty(g.Key)))
                {
                    _logger.LogInformation($"Processing duplicates for TransactionId: {group.Key}");

                    // Sort by Timestamp to keep the most recent one
                    var sortedTransactions = group.OrderByDescending(t => t.Timestamp).ToList();

                    // Keep the first one (most recent) and delete the rest
                    var keepTransaction = sortedTransactions.First();
                    var deleteTransactions = sortedTransactions.Skip(1).ToList();

                    _logger.LogInformation($"Keeping transaction with Id: {keepTransaction.Id}, deleting {deleteTransactions.Count} duplicates");

                    foreach (var transaction in deleteTransactions)
                    {
                        await _context.Transactions.DeleteOneAsync(t => t.Id == transaction.Id);
                        _logger.LogInformation($"Deleted duplicate transaction with Id: {transaction.Id}");
                    }
                }

                _logger.LogInformation("Completed cleanup of duplicate transactions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicate transactions");
                throw;
            }
        }

        /// <summary>
        /// Removes duplicate records from the MonthlyVehicles collection based on VehicleId
        /// </summary>
        public async Task CleanupDuplicateMonthlyVehiclesAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of duplicate monthly vehicles...");

                // Get all monthly vehicles
                var monthlyVehicles = await _context.MonthlyVehicles.Find(_ => true).ToListAsync();

                // Group monthly vehicles by VehicleId
                var groupedMonthlyVehicles = monthlyVehicles.GroupBy(mv => mv.VehicleId)
                    .Where(g => g.Count() > 1)
                    .ToList();

                _logger.LogInformation($"Found {groupedMonthlyVehicles.Count} monthly vehicle IDs with duplicates");

                foreach (var group in groupedMonthlyVehicles)
                {
                    _logger.LogInformation($"Processing duplicates for VehicleId: {group.Key}");

                    // Sort by EndDate to keep the one with the latest end date (most likely to be valid)
                    var sortedMonthlyVehicles = group.OrderByDescending(mv => mv.EndDate)
                        .ToList();

                    // Keep the first one (most recent) and delete the rest
                    var keepMonthlyVehicle = sortedMonthlyVehicles.First();
                    var deleteMonthlyVehicles = sortedMonthlyVehicles.Skip(1).ToList();

                    _logger.LogInformation($"Keeping monthly vehicle with Id: {keepMonthlyVehicle.Id}, deleting {deleteMonthlyVehicles.Count} duplicates");

                    foreach (var monthlyVehicle in deleteMonthlyVehicles)
                    {
                        await _context.MonthlyVehicles.DeleteOneAsync(mv => mv.Id == monthlyVehicle.Id);
                        _logger.LogInformation($"Deleted duplicate monthly vehicle with Id: {monthlyVehicle.Id}");
                    }
                }

                _logger.LogInformation("Completed cleanup of duplicate monthly vehicles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicate monthly vehicles");
                throw;
            }
        }

        /// <summary>
        /// Removes duplicate records from the ParkingSlots collection based on SlotId
        /// </summary>
        public async Task CleanupDuplicateParkingSlotsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of duplicate parking slots...");

                // Get all parking slots
                var parkingSlots = await _context.ParkingSlots.Find(_ => true).ToListAsync();

                // Group parking slots by SlotId
                var groupedParkingSlots = parkingSlots.GroupBy(ps => ps.SlotId)
                    .Where(g => g.Count() > 1)
                    .ToList();

                _logger.LogInformation($"Found {groupedParkingSlots.Count} parking slot IDs with duplicates");

                foreach (var group in groupedParkingSlots)
                {
                    _logger.LogInformation($"Processing duplicates for SlotId: {group.Key}");

                    // Sort by CreatedAt to keep the most recent one
                    var sortedParkingSlots = group.OrderByDescending(ps => ps.CreatedAt ?? DateTime.MinValue)
                        .ToList();

                    // Keep the first one (most recent) and delete the rest
                    var keepParkingSlot = sortedParkingSlots.First();
                    var deleteParkingSlots = sortedParkingSlots.Skip(1).ToList();

                    _logger.LogInformation($"Keeping parking slot with Id: {keepParkingSlot.Id}, deleting {deleteParkingSlots.Count} duplicates");

                    foreach (var parkingSlot in deleteParkingSlots)
                    {
                        await _context.ParkingSlots.DeleteOneAsync(ps => ps.Id == parkingSlot.Id);
                        _logger.LogInformation($"Deleted duplicate parking slot with Id: {parkingSlot.Id}");
                    }
                }

                _logger.LogInformation("Completed cleanup of duplicate parking slots");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicate parking slots");
                throw;
            }
        }

        /// <summary>
        /// Runs all cleanup operations to remove duplicates from all collections
        /// </summary>
        public async Task CleanupAllDuplicatesAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of all duplicate records...");

                await CleanupDuplicateVehiclesAsync();
                await CleanupDuplicateTransactionsAsync();
                await CleanupDuplicateMonthlyVehiclesAsync();
                await CleanupDuplicateParkingSlotsAsync();

                _logger.LogInformation("Completed cleanup of all duplicate records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicate records");
                throw;
            }
        }

        /// <summary>
        /// Fixes the specific issue with duplicate vehicles having vehicleId M001
        /// </summary>
        public async Task FixM001DuplicateAsync()
        {
            try
            {
                _logger.LogInformation("Starting fix for M001 duplicate vehicles...");

                // First, drop the unique index on vehicleId if it exists
                try
                {
                    var indexKeys = Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.VehicleId);
                    var indexOptions = new CreateIndexOptions { Name = "idx_vehicleId" };
                    var indexModel = new CreateIndexModel<Models.Vehicle>(indexKeys, indexOptions);

                    var vehiclesCollection = _context.Vehicles;
                    await vehiclesCollection.Indexes.DropOneAsync("idx_vehicleId");
                    _logger.LogInformation("Dropped idx_vehicleId index");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Could not drop idx_vehicleId index: {ex.Message}");
                }

                // Get all vehicles with vehicleId M001
                var filter = Builders<Models.Vehicle>.Filter.Eq(v => v.VehicleId, "M001");
                var duplicates = await _context.Vehicles.Find(filter).ToListAsync();

                _logger.LogInformation($"Found {duplicates.Count} vehicles with vehicleId M001");

                if (duplicates.Count > 1)
                {
                    // Sort by CreatedAt (newest first)
                    var sortedVehicles = duplicates.OrderByDescending(v => v.CreatedAt ?? DateTime.MinValue).ToList();

                    // Keep the most recent one
                    var keepVehicle = sortedVehicles.First();
                    _logger.LogInformation($"Keeping the most recent vehicle with _id: {keepVehicle.Id}");

                    // Remove the others
                    for (int i = 1; i < sortedVehicles.Count; i++)
                    {
                        var deleteFilter = Builders<Models.Vehicle>.Filter.Eq(v => v.Id, sortedVehicles[i].Id);
                        var result = await _context.Vehicles.DeleteOneAsync(deleteFilter);
                        _logger.LogInformation($"Removed duplicate vehicle with _id: {sortedVehicles[i].Id}, DeleteResult: {result.DeletedCount}");
                    }

                    _logger.LogInformation("Duplicate vehicles removed successfully");
                }
                else
                {
                    _logger.LogInformation("No duplicates found, nothing to fix");
                }

                // Recreate the index
                try
                {
                    var indexKeys = Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.VehicleId);
                    var indexOptions = new CreateIndexOptions { Name = "idx_vehicleId", Unique = true };
                    var indexModel = new CreateIndexModel<Models.Vehicle>(indexKeys, indexOptions);

                    var vehiclesCollection = _context.Vehicles;
                    await vehiclesCollection.Indexes.CreateOneAsync(indexModel);
                    _logger.LogInformation("Recreated idx_vehicleId index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recreating idx_vehicleId index");
                }

                _logger.LogInformation("Completed fix for M001 duplicate vehicles");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing M001 duplicate vehicles");
                throw;
            }
        }
    }
}
