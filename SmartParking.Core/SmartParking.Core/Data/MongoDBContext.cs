using MongoDB.Driver;
using SmartParking.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartParking.Core.Data
{
    public class MongoDBContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;
        private readonly string _databaseName;
        private readonly ILogger<MongoDBContext> _logger;

        public MongoDBContext(IConfiguration configuration, ILogger<MongoDBContext> logger)
        {
            _logger = logger;

            // Get connection string and database name from configuration
            var connectionString = configuration.GetConnectionString("MongoDb");
            _databaseName = configuration.GetSection("DatabaseSettings")["DatabaseName"];

            // Create MongoDB client and get database
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(_databaseName);

            _logger.LogInformation($"MongoDB context initialized with database: {_databaseName}");
        }

        // Methods to expose client and database name for index creation
        public MongoClient GetClient() => _client;
        public string GetDatabaseName() => _databaseName;

        public IMongoCollection<Vehicle> Vehicles => _database.GetCollection<Vehicle>("Vehicles");
        public IMongoCollection<ParkingSlot> ParkingSlots => _database.GetCollection<ParkingSlot>("ParkingSlots");
        public IMongoCollection<Transaction> Transactions => _database.GetCollection<Transaction>("Transactions");
        public IMongoCollection<MonthlyVehicle> MonthlyVehicles => _database.GetCollection<MonthlyVehicle>("MonthlyVehicles");
        public IMongoCollection<PendingRegistration> PendingRegistrations => _database.GetCollection<PendingRegistration>("PendingRegistrations");
        public IMongoCollection<PendingRenewal> PendingRenewals => _database.GetCollection<PendingRenewal>("PendingRenewals");
        public IMongoCollection<SystemSettings> SystemSettings => _database.GetCollection<SystemSettings>("SystemSettings");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
