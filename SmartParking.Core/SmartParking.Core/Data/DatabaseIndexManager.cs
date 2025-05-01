using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace SmartParking.Core.Data
{
    public class DatabaseIndexManager
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<DatabaseIndexManager> _logger;

        public DatabaseIndexManager(MongoDBContext context, ILogger<DatabaseIndexManager> logger)
        {
            var client = context.GetClient();
            _database = client.GetDatabase(context.GetDatabaseName());
            _logger = logger;
        }

        public async Task CreateIndexesAsync()
        {
            try
            {
                _logger.LogInformation("Creating database indexes...");

                // Create indexes for Transactions collection
                await CreateTransactionIndexesAsync();

                // Create indexes for Vehicles collection
                await CreateVehicleIndexesAsync();

                // Create indexes for ParkingSlots collection
                await CreateParkingSlotIndexesAsync();

                // Create indexes for MonthlyVehicles collection
                await CreateMonthlyVehicleIndexesAsync();

                _logger.LogInformation("Database indexes created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database indexes");
            }
        }

        private async Task CreateTransactionIndexesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Models.Transaction>("Transactions");

                // Index for TransactionId (for quick lookups)
                var transactionIdIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.TransactionId),
                    new CreateIndexOptions { Unique = true, Name = "idx_transactionId" }
                );

                // Index for IdempotencyKey (for duplicate prevention)
                var idempotencyKeyIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.IdempotencyKey),
                    new CreateIndexOptions { Unique = true, Name = "idx_idempotencyKey" }
                );

                // Index for VehicleId (for filtering by vehicle)
                var vehicleIdIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.VehicleId),
                    new CreateIndexOptions { Name = "idx_vehicleId" }
                );

                // Index for Status (for filtering by status)
                var statusIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.Status),
                    new CreateIndexOptions { Name = "idx_status" }
                );

                // Index for Timestamp (for date range queries)
                var timestampIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.Timestamp),
                    new CreateIndexOptions { Name = "idx_timestamp" }
                );

                // Index for ExpiresAt (for finding expired transactions)
                var expiresAtIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.ExpiresAt),
                    new CreateIndexOptions { Name = "idx_expiresAt" }
                );

                // Index for PaymentMethod (for filtering by payment method)
                var paymentMethodIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys.Ascending(t => t.PaymentMethod),
                    new CreateIndexOptions { Name = "idx_paymentMethod" }
                );

                // Compound index for Status and Timestamp (for finding pending transactions that have timed out)
                var statusTimestampIndexModel = new CreateIndexModel<Models.Transaction>(
                    Builders<Models.Transaction>.IndexKeys
                        .Ascending(t => t.Status)
                        .Ascending(t => t.Timestamp),
                    new CreateIndexOptions { Name = "idx_status_timestamp" }
                );

                // Create all indexes
                await collection.Indexes.CreateManyAsync(new[] {
                    transactionIdIndexModel,
                    idempotencyKeyIndexModel,
                    vehicleIdIndexModel,
                    statusIndexModel,
                    timestampIndexModel,
                    expiresAtIndexModel,
                    paymentMethodIndexModel,
                    statusTimestampIndexModel
                });

                _logger.LogInformation("Created indexes for Transactions collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for Transactions collection");
            }
        }

        private async Task CreateVehicleIndexesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Models.Vehicle>("Vehicles");

                // First, try to drop existing indexes to avoid duplicate key errors
                try
                {
                    await collection.Indexes.DropOneAsync("idx_vehicleId");
                    _logger.LogInformation("Dropped existing idx_vehicleId index");
                }
                catch (Exception ex)
                {
                    // Index might not exist, which is fine
                    _logger.LogInformation($"Could not drop idx_vehicleId index: {ex.Message}");
                }

                // Index for VehicleId (for quick lookups)
                var vehicleIdIndexModel = new CreateIndexModel<Models.Vehicle>(
                    Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.VehicleId),
                    new CreateIndexOptions { Unique = true, Name = "idx_vehicleId" }
                );

                // Index for LicensePlate (for quick lookups)
                var licensePlateIndexModel = new CreateIndexModel<Models.Vehicle>(
                    Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.LicensePlate),
                    new CreateIndexOptions { Name = "idx_licensePlate" }
                );

                // Index for Status (for filtering by status)
                var statusIndexModel = new CreateIndexModel<Models.Vehicle>(
                    Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.Status),
                    new CreateIndexOptions { Name = "idx_status" }
                );

                // Index for SlotId (for finding vehicles in a specific slot)
                var slotIdIndexModel = new CreateIndexModel<Models.Vehicle>(
                    Builders<Models.Vehicle>.IndexKeys.Ascending(v => v.SlotId),
                    new CreateIndexOptions { Name = "idx_slotId" }
                );

                // Create all indexes
                await collection.Indexes.CreateManyAsync(new[] {
                    vehicleIdIndexModel,
                    licensePlateIndexModel,
                    statusIndexModel,
                    slotIdIndexModel
                });

                _logger.LogInformation("Created indexes for Vehicles collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for Vehicles collection");
            }
        }

        private async Task CreateParkingSlotIndexesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Models.ParkingSlot>("ParkingSlots");

                // Index for SlotId (for quick lookups)
                var slotIdIndexModel = new CreateIndexModel<Models.ParkingSlot>(
                    Builders<Models.ParkingSlot>.IndexKeys.Ascending(s => s.SlotId),
                    new CreateIndexOptions { Unique = true, Name = "idx_slotId" }
                );

                // Index for Status (for finding available slots)
                var statusIndexModel = new CreateIndexModel<Models.ParkingSlot>(
                    Builders<Models.ParkingSlot>.IndexKeys.Ascending(s => s.Status),
                    new CreateIndexOptions { Name = "idx_status" }
                );

                // Index for Type (for filtering by slot type)
                var typeIndexModel = new CreateIndexModel<Models.ParkingSlot>(
                    Builders<Models.ParkingSlot>.IndexKeys.Ascending(s => s.Type),
                    new CreateIndexOptions { Name = "idx_type" }
                );

                // Compound index for Status and Type (for finding available slots of a specific type)
                var statusTypeIndexModel = new CreateIndexModel<Models.ParkingSlot>(
                    Builders<Models.ParkingSlot>.IndexKeys
                        .Ascending(s => s.Status)
                        .Ascending(s => s.Type),
                    new CreateIndexOptions { Name = "idx_status_type" }
                );

                // Create all indexes
                await collection.Indexes.CreateManyAsync(new[] {
                    slotIdIndexModel,
                    statusIndexModel,
                    typeIndexModel,
                    statusTypeIndexModel
                });

                _logger.LogInformation("Created indexes for ParkingSlots collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for ParkingSlots collection");
            }
        }

        private async Task CreateMonthlyVehicleIndexesAsync()
        {
            try
            {
                var collection = _database.GetCollection<Models.MonthlyVehicle>("MonthlyVehicles");

                // Index for VehicleId (for quick lookups)
                var vehicleIdIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys.Ascending(m => m.VehicleId),
                    new CreateIndexOptions { Unique = true, Name = "idx_vehicleId" }
                );

                // Index for LicensePlate (for quick lookups)
                var licensePlateIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys.Ascending(m => m.LicensePlate),
                    new CreateIndexOptions { Name = "idx_licensePlate" }
                );

                // Index for Status (for filtering by status)
                var statusIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys.Ascending(m => m.Status),
                    new CreateIndexOptions { Name = "idx_status" }
                );

                // Index for EndDate (for finding expired vehicles)
                var endDateIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys.Ascending(m => m.EndDate),
                    new CreateIndexOptions { Name = "idx_endDate" }
                );

                // Index for FixedSlotId (for finding vehicles with a specific fixed slot)
                var fixedSlotIdIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys.Ascending(m => m.FixedSlotId),
                    new CreateIndexOptions { Name = "idx_fixedSlotId" }
                );

                // Compound index for Status and EndDate (for finding valid vehicles that are about to expire)
                var statusEndDateIndexModel = new CreateIndexModel<Models.MonthlyVehicle>(
                    Builders<Models.MonthlyVehicle>.IndexKeys
                        .Ascending(m => m.Status)
                        .Ascending(m => m.EndDate),
                    new CreateIndexOptions { Name = "idx_status_endDate" }
                );

                // Create all indexes
                await collection.Indexes.CreateManyAsync(new[] {
                    vehicleIdIndexModel,
                    licensePlateIndexModel,
                    statusIndexModel,
                    endDateIndexModel,
                    fixedSlotIdIndexModel,
                    statusEndDateIndexModel
                });

                _logger.LogInformation("Created indexes for MonthlyVehicles collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating indexes for MonthlyVehicles collection");
            }
        }
    }
}
