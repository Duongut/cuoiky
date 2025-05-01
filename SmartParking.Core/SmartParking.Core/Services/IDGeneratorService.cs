using MongoDB.Driver;
using SmartParking.Core.Data;
using SmartParking.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class IDGeneratorService
    {
        private readonly MongoDBContext _context;

        public IDGeneratorService(MongoDBContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateVehicleId(string vehicleType)
        {
            // Determine prefix based on vehicle type
            string prefix = vehicleType.ToUpper() == "CAR" ? "C" : "M";

            // Get the latest ID with the same prefix
            var filter = Builders<Vehicle>.Filter.Regex(v => v.VehicleId, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
            var sortDefinition = Builders<Vehicle>.Sort.Descending(v => v.VehicleId);

            var latestVehicle = await _context.Vehicles
                .Find(filter)
                .Sort(sortDefinition)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestVehicle != null)
            {
                // Extract the number part from the latest ID
                string numberPart = latestVehicle.VehicleId.Substring(1);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Format: M001, C001, etc.
            return $"{prefix}{nextNumber:D3}";
        }

        public async Task<string> GenerateMonthlyVehicleId(string vehicleType)
        {
            // Determine prefix based on vehicle type (MM for monthly motorcycle, MC for monthly car)
            string prefix = vehicleType.ToUpper() == "CAR" ? "MC" : "MM";

            // Get the latest ID with the same prefix
            var filter = Builders<MonthlyVehicle>.Filter.Regex(v => v.VehicleId, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
            var sortDefinition = Builders<MonthlyVehicle>.Sort.Descending(v => v.VehicleId);

            var latestVehicle = await _context.MonthlyVehicles
                .Find(filter)
                .Sort(sortDefinition)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestVehicle != null)
            {
                // Extract the number part from the latest ID
                string numberPart = latestVehicle.VehicleId.Substring(2);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Format: MM001, MC001, etc.
            return $"{prefix}{nextNumber:D3}";
        }

        public async Task<string> GenerateEmployeeIdAsync(string role)
        {
            // Determine prefix based on role
            string prefix = role.ToUpper() == "ADMIN" ? "ADM" : "EMP";

            // Get the latest ID with the same prefix
            var filter = Builders<User>.Filter.Regex(u => u.EmployeeId, new MongoDB.Bson.BsonRegularExpression($"^{prefix}"));
            var sortDefinition = Builders<User>.Sort.Descending(u => u.EmployeeId);

            var latestUser = await _context.Users
                .Find(filter)
                .Sort(sortDefinition)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (latestUser != null)
            {
                // Extract the number part from the latest ID
                string numberPart = latestUser.EmployeeId.Substring(3);
                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            // Format: ADM001, EMP001, etc.
            return $"{prefix}{nextNumber:D3}";
        }
    }
}
