using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SmartParking.Core.Data;
using SmartParking.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SmartParking.Core.Services
{
    public class MonthlyVehicleService
    {
        private readonly MongoDBContext _context;
        private readonly ILogger<MonthlyVehicleService> _logger;
        private readonly IDGeneratorService _idGenerator;
        private readonly TransactionService _transactionService;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly SettingsService _settingsService;

        public MonthlyVehicleService(
            MongoDBContext context,
            ILogger<MonthlyVehicleService> logger,
            IDGeneratorService idGenerator,
            TransactionService transactionService,
            IConfiguration configuration,
            EmailService emailService,
            SettingsService settingsService = null) // Optional to avoid circular dependency
        {
            _context = context;
            _logger = logger;
            _idGenerator = idGenerator;
            _transactionService = transactionService;
            _configuration = configuration;
            _emailService = emailService;
            _settingsService = settingsService;
        }

        public async Task<List<MonthlyVehicle>> GetAllMonthlyVehiclesAsync()
        {
            return await _context.MonthlyVehicles.Find(FilterDefinition<MonthlyVehicle>.Empty).ToListAsync();
        }

        public async Task<MonthlyVehicle> GetMonthlyVehicleByIdAsync(string id)
        {
            return await _context.MonthlyVehicles.Find(v => v.Id == id).FirstOrDefaultAsync();
        }

        public async Task<MonthlyVehicle> GetMonthlyVehicleByVehicleIdAsync(string vehicleId)
        {
            return await _context.MonthlyVehicles.Find(v => v.VehicleId == vehicleId).FirstOrDefaultAsync();
        }

        public async Task<MonthlyVehicle> GetMonthlyVehicleByLicensePlateAsync(string licensePlate)
        {
            return await _context.MonthlyVehicles.Find(v => v.LicensePlate == licensePlate && v.Status == "VALID").FirstOrDefaultAsync();
        }

        public async Task<List<MonthlyVehicle>> GetValidMonthlyVehiclesAsync()
        {
            return await _context.MonthlyVehicles.Find(v => v.Status == "VALID").ToListAsync();
        }

        public async Task<List<MonthlyVehicle>> GetExpiredMonthlyVehiclesAsync()
        {
            return await _context.MonthlyVehicles.Find(v => v.Status == "EXPIRED").ToListAsync();
        }

        public async Task<List<MonthlyVehicle>> GetMonthlyVehiclesByStatusAsync(string status)
        {
            return await _context.MonthlyVehicles.Find(v => v.Status == status).ToListAsync();
        }

        public async Task<ParkingSlot> FindAvailableFixedSlotAsync(string vehicleType)
        {
            string slotType = vehicleType.ToUpper() == "CAR" ? "CAR" : "MOTORBIKE";

            // Find all slots of the correct type
            var allSlots = await _context.ParkingSlots
                .Find(s => s.Type == slotType)
                .ToListAsync();

            // Find all monthly vehicles with fixed slots
            var monthlyVehicles = await _context.MonthlyVehicles
                .Find(v => v.Status == "VALID" && v.VehicleType == vehicleType.ToUpper() && !string.IsNullOrEmpty(v.FixedSlotId))
                .ToListAsync();

            // Create a set of already assigned fixed slots
            var assignedSlots = new HashSet<string>(monthlyVehicles.Select(v => v.FixedSlotId));

            // Find the first available slot that is not assigned as a fixed slot to any monthly vehicle
            foreach (var slot in allSlots)
            {
                if (!assignedSlots.Contains(slot.SlotId))
                {
                    return slot;
                }
            }

            throw new Exception($"No available fixed parking slots for {vehicleType}");
        }

        public async Task<ParkingSlot> AssignFixedParkingSlotAsync(string vehicleType)
        {
            var slot = await FindAvailableFixedSlotAsync(vehicleType);

            // Mark the slot as reserved for monthly vehicle
            var filter = Builders<ParkingSlot>.Filter.Eq(s => s.SlotId, slot.SlotId);
            var update = Builders<ParkingSlot>.Update
                .Set(s => s.Status, "RESERVED")
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _context.ParkingSlots.UpdateOneAsync(filter, update);

            _logger.LogInformation($"Fixed parking slot {slot.SlotId} status updated to RESERVED for monthly vehicle");

            _logger.LogInformation($"Fixed parking slot {slot.SlotId} assigned for monthly vehicle");

            return slot;
        }

        public async Task<ParkingSlot> AssignFixedParkingSlotByIdAsync(string slotId)
        {
            // Get the slot
            var slot = await _context.ParkingSlots.Find(s => s.SlotId == slotId).FirstOrDefaultAsync();

            if (slot == null)
            {
                throw new Exception($"Parking slot with ID {slotId} not found");
            }

            // Check if the slot is available
            if (slot.Status != "AVAILABLE" && slot.Status != "RESERVED")
            {
                _logger.LogWarning($"Attempting to assign slot {slotId} that is not available. Current status: {slot.Status}");
            }

            // Mark the slot as reserved for monthly vehicle
            var filter = Builders<ParkingSlot>.Filter.Eq(s => s.SlotId, slotId);
            var update = Builders<ParkingSlot>.Update
                .Set(s => s.Status, "RESERVED")
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _context.ParkingSlots.UpdateOneAsync(filter, update);

            _logger.LogInformation($"Fixed parking slot {slotId} status updated to RESERVED for monthly vehicle");

            _logger.LogInformation($"Fixed parking slot {slotId} assigned for monthly vehicle");

            return slot;
        }

        public async Task<PendingRegistration> CreatePendingRegistrationAsync(
            string licensePlate,
            string vehicleType,
            string customerName,
            string customerPhone,
            string customerEmail,
            int packageDuration,
            decimal packageAmount,
            int discountPercentage)
        {
            // Check if license plate is already registered
            var existingVehicle = await GetMonthlyVehicleByLicensePlateAsync(licensePlate);
            if (existingVehicle != null)
            {
                throw new Exception($"License plate {licensePlate} is already registered with a valid monthly package");
            }

            // Generate a unique ID for the monthly vehicle
            string vehicleId = await _idGenerator.GenerateMonthlyVehicleId(vehicleType);

            // Find an available fixed parking slot (but don't assign it yet)
            var availableSlot = await FindAvailableFixedSlotAsync(vehicleType);
            if (availableSlot == null)
            {
                throw new Exception($"No available fixed parking slots for {vehicleType}");
            }

            // Create a new pending registration record
            var pendingRegistration = new PendingRegistration
            {
                VehicleId = vehicleId,
                LicensePlate = licensePlate,
                VehicleType = vehicleType.ToUpper(),
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                PackageDuration = packageDuration,
                PackageAmount = packageAmount,
                DiscountPercentage = discountPercentage,
                CreatedAt = DateTime.Now,
                Status = "PENDING",
                FixedSlotId = availableSlot.SlotId
            };

            // Save the pending registration to the database
            await _context.PendingRegistrations.InsertOneAsync(pendingRegistration);

            _logger.LogInformation($"Created pending registration for {licensePlate} with ID {pendingRegistration.Id}");

            return pendingRegistration;
        }

        public async Task<MonthlyVehicle> CompleteRegistrationAsync(string registrationId, string transactionId)
        {
            // Find the pending registration
            var pendingRegistration = await _context.PendingRegistrations
                .Find(r => r.Id == registrationId && r.Status == "PENDING")
                .FirstOrDefaultAsync();

            if (pendingRegistration == null)
            {
                throw new Exception($"Pending registration with ID {registrationId} not found or already processed");
            }

            // Update the pending registration status
            var filter = Builders<PendingRegistration>.Filter.Eq(r => r.Id, registrationId);
            var update = Builders<PendingRegistration>.Update
                .Set(r => r.Status, "COMPLETED")
                .Set(r => r.TransactionId, transactionId);

            await _context.PendingRegistrations.UpdateOneAsync(filter, update);

            // Assign the fixed parking slot
            await AssignFixedParkingSlotByIdAsync(pendingRegistration.FixedSlotId);

            // Calculate start and end dates
            var startDate = DateTime.Now;
            var endDate = startDate.AddMonths(pendingRegistration.PackageDuration);

            // Create a new monthly vehicle record
            var monthlyVehicle = new MonthlyVehicle
            {
                VehicleId = pendingRegistration.VehicleId,
                LicensePlate = pendingRegistration.LicensePlate,
                VehicleType = pendingRegistration.VehicleType,
                CustomerName = pendingRegistration.CustomerName,
                CustomerPhone = pendingRegistration.CustomerPhone,
                CustomerEmail = pendingRegistration.CustomerEmail,
                StartDate = startDate,
                EndDate = endDate,
                Status = "VALID",
                RegistrationDate = DateTime.Now,
                PackageDuration = pendingRegistration.PackageDuration,
                PackageAmount = pendingRegistration.PackageAmount,
                DiscountPercentage = pendingRegistration.DiscountPercentage,
                FixedSlotId = pendingRegistration.FixedSlotId
            };

            // Save the monthly vehicle to the database
            await _context.MonthlyVehicles.InsertOneAsync(monthlyVehicle);

            // Send confirmation email
            try
            {
                await _emailService.SendRegistrationConfirmationAsync(monthlyVehicle);
                _logger.LogInformation($"Registration confirmation email sent to {monthlyVehicle.CustomerEmail} for vehicle {monthlyVehicle.LicensePlate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration confirmation email to {monthlyVehicle.CustomerEmail}");
                // Continue execution even if email fails
            }

            return monthlyVehicle;
        }

        public async Task<MonthlyVehicle> RegisterMonthlyVehicleAsync(
            string licensePlate,
            string vehicleType,
            string customerName,
            string customerPhone,
            string customerEmail,
            int packageDuration,
            decimal packageAmount,
            int discountPercentage)
        {
            // Check if license plate is already registered
            var existingVehicle = await GetMonthlyVehicleByLicensePlateAsync(licensePlate);
            if (existingVehicle != null)
            {
                throw new Exception($"License plate {licensePlate} is already registered with a valid monthly package");
            }

            // Generate a unique ID for the monthly vehicle
            string vehicleId = await _idGenerator.GenerateMonthlyVehicleId(vehicleType);

            // Assign a fixed parking slot
            var fixedSlot = await AssignFixedParkingSlotAsync(vehicleType);

            // Calculate start and end dates
            var startDate = DateTime.Now;
            var endDate = startDate.AddMonths(packageDuration);

            // Create a new monthly vehicle record
            var monthlyVehicle = new MonthlyVehicle
            {
                VehicleId = vehicleId,
                LicensePlate = licensePlate,
                VehicleType = vehicleType.ToUpper(),
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                StartDate = startDate,
                EndDate = endDate,
                Status = "VALID",
                RegistrationDate = DateTime.Now,
                PackageDuration = packageDuration,
                PackageAmount = packageAmount,
                DiscountPercentage = discountPercentage,
                FixedSlotId = fixedSlot.SlotId
            };

            // Save the monthly vehicle to the database
            await _context.MonthlyVehicles.InsertOneAsync(monthlyVehicle);

            // Send confirmation email
            try
            {
                await _emailService.SendRegistrationConfirmationAsync(monthlyVehicle);
                _logger.LogInformation($"Registration confirmation email sent to {monthlyVehicle.CustomerEmail} for vehicle {monthlyVehicle.LicensePlate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send registration confirmation email to {monthlyVehicle.CustomerEmail}");
                // Continue execution even if email fails
            }

            return monthlyVehicle;
        }

        public async Task<MonthlyVehicle> RenewMonthlyVehicleAsync(
            string id,
            int packageDuration,
            decimal packageAmount,
            int discountPercentage)
        {
            // Find the monthly vehicle
            var monthlyVehicle = await GetMonthlyVehicleByIdAsync(id);
            if (monthlyVehicle == null)
            {
                throw new Exception($"Monthly vehicle with ID {id} not found");
            }

            // Calculate new end date
            var newEndDate = monthlyVehicle.Status == "VALID"
                ? monthlyVehicle.EndDate.AddMonths(packageDuration)
                : DateTime.Now.AddMonths(packageDuration);

            // Check if we need to reassign a fixed slot (for expired vehicles)
            if (monthlyVehicle.Status == "EXPIRED" || string.IsNullOrEmpty(monthlyVehicle.FixedSlotId))
            {
                // Assign a new fixed parking slot
                var fixedSlot = await AssignFixedParkingSlotAsync(monthlyVehicle.VehicleType);

                // Update the monthly vehicle with the new fixed slot
                var filter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, id);
                var update = Builders<MonthlyVehicle>.Update
                    .Set(v => v.EndDate, newEndDate)
                    .Set(v => v.Status, "VALID")
                    .Set(v => v.LastRenewalDate, DateTime.Now)
                    .Set(v => v.PackageDuration, packageDuration)
                    .Set(v => v.PackageAmount, packageAmount)
                    .Set(v => v.DiscountPercentage, discountPercentage)
                    .Set(v => v.FixedSlotId, fixedSlot.SlotId);

                await _context.MonthlyVehicles.UpdateOneAsync(filter, update);
            }
            else
            {
                // Just update the existing monthly vehicle
                var filter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, id);
                var update = Builders<MonthlyVehicle>.Update
                    .Set(v => v.EndDate, newEndDate)
                    .Set(v => v.Status, "VALID")
                    .Set(v => v.LastRenewalDate, DateTime.Now)
                    .Set(v => v.PackageDuration, packageDuration)
                    .Set(v => v.PackageAmount, packageAmount)
                    .Set(v => v.DiscountPercentage, discountPercentage);

                await _context.MonthlyVehicles.UpdateOneAsync(filter, update);
            }

            // Get the updated monthly vehicle
            var updatedVehicle = await GetMonthlyVehicleByIdAsync(id);

            // Send renewal confirmation email
            try
            {
                await _emailService.SendRenewalConfirmationAsync(updatedVehicle);
                _logger.LogInformation($"Renewal confirmation email sent to {updatedVehicle.CustomerEmail} for vehicle {updatedVehicle.LicensePlate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send renewal confirmation email to {updatedVehicle.CustomerEmail}");
                // Continue execution even if email fails
            }

            return updatedVehicle;
        }

        public async Task<PendingRenewal> CreatePendingRenewalAsync(
            string monthlyVehicleId,
            int packageDuration,
            decimal packageAmount,
            int discountPercentage)
        {
            // Find the monthly vehicle
            var monthlyVehicle = await GetMonthlyVehicleByIdAsync(monthlyVehicleId);
            if (monthlyVehicle == null)
            {
                throw new Exception($"Monthly vehicle with ID {monthlyVehicleId} not found");
            }

            // Check if we need to find a new fixed slot (for expired vehicles)
            string fixedSlotId = monthlyVehicle.FixedSlotId;

            if (monthlyVehicle.Status == "EXPIRED" || string.IsNullOrEmpty(monthlyVehicle.FixedSlotId))
            {
                // Find an available fixed parking slot (but don't assign it yet)
                var availableSlot = await FindAvailableFixedSlotAsync(monthlyVehicle.VehicleType);
                if (availableSlot == null)
                {
                    throw new Exception($"No available fixed parking slots for {monthlyVehicle.VehicleType}");
                }

                fixedSlotId = availableSlot.SlotId;
            }

            // Create a new pending renewal record
            var pendingRenewal = new PendingRenewal
            {
                MonthlyVehicleId = monthlyVehicleId,
                VehicleId = monthlyVehicle.VehicleId,
                LicensePlate = monthlyVehicle.LicensePlate,
                VehicleType = monthlyVehicle.VehicleType,
                CustomerName = monthlyVehicle.CustomerName,
                CustomerPhone = monthlyVehicle.CustomerPhone,
                CustomerEmail = monthlyVehicle.CustomerEmail,
                PackageDuration = packageDuration,
                PackageAmount = packageAmount,
                DiscountPercentage = discountPercentage,
                CreatedAt = DateTime.Now,
                Status = "PENDING",
                FixedSlotId = fixedSlotId
            };

            // Save the pending renewal to the database
            await _context.PendingRenewals.InsertOneAsync(pendingRenewal);

            _logger.LogInformation($"Created pending renewal for {monthlyVehicle.LicensePlate} with ID {pendingRenewal.Id}");

            return pendingRenewal;
        }

        public async Task<MonthlyVehicle> CompleteRenewalAsync(string renewalId, string transactionId)
        {
            // Find the pending renewal
            var pendingRenewal = await _context.PendingRenewals
                .Find(r => r.Id == renewalId && r.Status == "PENDING")
                .FirstOrDefaultAsync();

            if (pendingRenewal == null)
            {
                throw new Exception($"Pending renewal with ID {renewalId} not found or already processed");
            }

            // Update the pending renewal status
            var filter = Builders<PendingRenewal>.Filter.Eq(r => r.Id, renewalId);
            var update = Builders<PendingRenewal>.Update
                .Set(r => r.Status, "COMPLETED")
                .Set(r => r.TransactionId, transactionId);

            await _context.PendingRenewals.UpdateOneAsync(filter, update);

            // Find the monthly vehicle
            var monthlyVehicle = await GetMonthlyVehicleByIdAsync(pendingRenewal.MonthlyVehicleId);
            if (monthlyVehicle == null)
            {
                throw new Exception($"Monthly vehicle with ID {pendingRenewal.MonthlyVehicleId} not found");
            }

            // Calculate new end date
            var newEndDate = monthlyVehicle.Status == "VALID"
                ? monthlyVehicle.EndDate.AddMonths(pendingRenewal.PackageDuration)
                : DateTime.Now.AddMonths(pendingRenewal.PackageDuration);

            // Check if we need to reassign a fixed slot (for expired vehicles)
            if (monthlyVehicle.Status == "EXPIRED" || string.IsNullOrEmpty(monthlyVehicle.FixedSlotId))
            {
                // Assign the fixed parking slot
                await AssignFixedParkingSlotByIdAsync(pendingRenewal.FixedSlotId);

                // Update the monthly vehicle with the new fixed slot
                var vehicleFilter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, pendingRenewal.MonthlyVehicleId);
                var vehicleUpdate = Builders<MonthlyVehicle>.Update
                    .Set(v => v.EndDate, newEndDate)
                    .Set(v => v.Status, "VALID")
                    .Set(v => v.LastRenewalDate, DateTime.Now)
                    .Set(v => v.PackageDuration, pendingRenewal.PackageDuration)
                    .Set(v => v.PackageAmount, pendingRenewal.PackageAmount)
                    .Set(v => v.DiscountPercentage, pendingRenewal.DiscountPercentage)
                    .Set(v => v.FixedSlotId, pendingRenewal.FixedSlotId);

                await _context.MonthlyVehicles.UpdateOneAsync(vehicleFilter, vehicleUpdate);
            }
            else
            {
                // Just update the existing monthly vehicle
                var vehicleFilter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, pendingRenewal.MonthlyVehicleId);
                var vehicleUpdate = Builders<MonthlyVehicle>.Update
                    .Set(v => v.EndDate, newEndDate)
                    .Set(v => v.Status, "VALID")
                    .Set(v => v.LastRenewalDate, DateTime.Now)
                    .Set(v => v.PackageDuration, pendingRenewal.PackageDuration)
                    .Set(v => v.PackageAmount, pendingRenewal.PackageAmount)
                    .Set(v => v.DiscountPercentage, pendingRenewal.DiscountPercentage);

                await _context.MonthlyVehicles.UpdateOneAsync(vehicleFilter, vehicleUpdate);
            }

            // Get the updated monthly vehicle
            var updatedVehicle = await GetMonthlyVehicleByIdAsync(pendingRenewal.MonthlyVehicleId);

            // Send renewal confirmation email
            try
            {
                await _emailService.SendRenewalConfirmationAsync(updatedVehicle);
                _logger.LogInformation($"Renewal confirmation email sent to {updatedVehicle.CustomerEmail} for vehicle {updatedVehicle.LicensePlate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send renewal confirmation email to {updatedVehicle.CustomerEmail}");
                // Continue execution even if email fails
            }

            return updatedVehicle;
        }

        public async Task<MonthlyVehicle> CancelMonthlyVehicleAsync(string id)
        {
            // Find the monthly vehicle
            var monthlyVehicle = await GetMonthlyVehicleByIdAsync(id);
            if (monthlyVehicle == null)
            {
                throw new Exception($"Monthly vehicle with ID {id} not found");
            }

            // Update the monthly vehicle
            var filter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, id);
            var update = Builders<MonthlyVehicle>.Update
                .Set(v => v.Status, "CANCELLED");

            await _context.MonthlyVehicles.UpdateOneAsync(filter, update);

            // Release the fixed parking slot if it exists
            if (!string.IsNullOrEmpty(monthlyVehicle.FixedSlotId))
            {
                var slotFilter = Builders<ParkingSlot>.Filter.Eq(s => s.SlotId, monthlyVehicle.FixedSlotId);
                var slotUpdate = Builders<ParkingSlot>.Update
                    .Set(s => s.Status, "AVAILABLE")
                    .Set(s => s.CurrentVehicleId, null);

                await _context.ParkingSlots.UpdateOneAsync(slotFilter, slotUpdate);
                _logger.LogInformation($"Fixed parking slot {monthlyVehicle.FixedSlotId} released after cancellation");
            }

            // Get the updated monthly vehicle
            var updatedVehicle = await GetMonthlyVehicleByIdAsync(id);

            // Send cancellation email
            try
            {
                await _emailService.SendCancellationNotificationAsync(updatedVehicle);
                _logger.LogInformation($"Cancellation email sent to {updatedVehicle.CustomerEmail} for vehicle {updatedVehicle.LicensePlate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send cancellation email to {updatedVehicle.CustomerEmail}");
                // Continue execution even if email fails
            }

            return updatedVehicle;
        }

        public async Task UpdateExpiredVehiclesAsync()
        {
            var now = DateTime.Now;

            // Find vehicles that are about to expire (within 3 days)
            var expiringVehicles = await _context.MonthlyVehicles.Find(
                v => v.Status == "VALID" &&
                v.EndDate > now &&
                v.EndDate < now.AddDays(3)
            ).ToListAsync();

            // Send expiration reminder emails
            foreach (var vehicle in expiringVehicles)
            {
                try
                {
                    await _emailService.SendExpirationReminderAsync(vehicle);
                    _logger.LogInformation($"Expiration reminder email sent to {vehicle.CustomerEmail} for vehicle {vehicle.LicensePlate}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send expiration reminder email to {vehicle.CustomerEmail}");
                    // Continue execution even if email fails
                }
            }

            // Find vehicles that have expired
            var expiredVehicles = await _context.MonthlyVehicles.Find(
                v => v.Status == "VALID" && v.EndDate < now
            ).ToListAsync();

            // Update expired vehicles and release their fixed slots
            foreach (var vehicle in expiredVehicles)
            {
                _logger.LogInformation($"Monthly vehicle {vehicle.VehicleId} (license plate: {vehicle.LicensePlate}) has expired. Updating status and releasing fixed slot.");

                // Update vehicle status
                var filter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Id, vehicle.Id);
                var update = Builders<MonthlyVehicle>.Update
                    .Set(v => v.Status, "EXPIRED");

                await _context.MonthlyVehicles.UpdateOneAsync(filter, update);

                // Release the fixed parking slot if it exists
                if (!string.IsNullOrEmpty(vehicle.FixedSlotId))
                {
                    // Check current status of the slot
                    var slot = await _context.ParkingSlots.Find(s => s.SlotId == vehicle.FixedSlotId).FirstOrDefaultAsync();
                    if (slot == null)
                    {
                        _logger.LogWarning($"Fixed parking slot {vehicle.FixedSlotId} not found for expired vehicle {vehicle.VehicleId}");
                        continue;
                    }

                    // If the slot is currently occupied, we should not change its status
                    if (slot.Status == "OCCUPIED")
                    {
                        _logger.LogWarning($"Fixed parking slot {vehicle.FixedSlotId} for expired vehicle {vehicle.VehicleId} is currently occupied. Will not change status until vehicle exits.");
                        continue;
                    }

                    // Set the slot status to AVAILABLE as it's no longer reserved for this monthly vehicle
                    var slotFilter = Builders<ParkingSlot>.Filter.Eq(s => s.SlotId, vehicle.FixedSlotId);
                    var slotUpdate = Builders<ParkingSlot>.Update
                        .Set(s => s.Status, "AVAILABLE")
                        .Set(s => s.CurrentVehicleId, null)
                        .Set(s => s.UpdatedAt, DateTime.UtcNow);

                    await _context.ParkingSlots.UpdateOneAsync(slotFilter, slotUpdate);
                    _logger.LogInformation($"Fixed parking slot {vehicle.FixedSlotId} released after expiration of monthly vehicle {vehicle.VehicleId}");
                }

                // Send expiration email
                try
                {
                    await _emailService.SendExpirationNotificationAsync(vehicle);
                    _logger.LogInformation($"Expiration email sent to {vehicle.CustomerEmail} for vehicle {vehicle.LicensePlate}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send expiration email to {vehicle.CustomerEmail}");
                    // Continue execution even if email fails
                }
            }
        }

        public async Task<bool> IsVehicleRegisteredMonthlyAsync(string licensePlate)
        {
            var monthlyVehicle = await GetMonthlyVehicleByLicensePlateAsync(licensePlate);
            return monthlyVehicle != null && monthlyVehicle.Status == "VALID";
        }

        public async Task<int> GetMonthlyVehiclesCountAsync()
        {
            var filter = Builders<MonthlyVehicle>.Filter.Eq(v => v.Status, "VALID");
            return (int)await _context.MonthlyVehicles.CountDocumentsAsync(filter);
        }

        public async Task<List<MonthlyVehicle>> GetExpiringVehiclesAsync(int days)
        {
            var now = DateTime.Now;
            var expiryDate = now.AddDays(days);

            var filter = Builders<MonthlyVehicle>.Filter.And(
                Builders<MonthlyVehicle>.Filter.Eq(v => v.Status, "VALID"),
                Builders<MonthlyVehicle>.Filter.Gte(v => v.EndDate, now),
                Builders<MonthlyVehicle>.Filter.Lte(v => v.EndDate, expiryDate)
            );

            return await _context.MonthlyVehicles.Find(filter).ToListAsync();
        }

        public async Task<decimal> CalculatePackagePrice(string vehicleType, int durationMonths)
        {
            try
            {
                if (_settingsService != null)
                {
                    // Get fee settings from database
                    var feeSettings = await _settingsService.GetParkingFeeSettingsAsync();

                    // Set base price based on vehicle type
                    decimal basePrice = vehicleType.ToUpper() == "CAR"
                        ? feeSettings.MonthlyCarFee
                        : feeSettings.MonthlyMotorbikeFee;

                    // Calculate total price without discount
                    decimal totalPrice = basePrice * durationMonths;

                    // Get discount percentage from settings
                    int discountPercentage = await _settingsService.GetDiscountPercentageAsync(durationMonths);

                    // Apply discount
                    decimal discountAmount = totalPrice * discountPercentage / 100;
                    decimal finalPrice = totalPrice - discountAmount;

                    return finalPrice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings from database. Using default values.");
            }

            // Fallback to hardcoded values if settings service is not available or fails
            decimal defaultBasePrice;

            // Set base price based on vehicle type
            if (vehicleType.ToUpper() == "CAR")
            {
                defaultBasePrice = 500000; // 500,000 VND per month for cars
            }
            else // MOTORCYCLE
            {
                defaultBasePrice = 100000; // 100,000 VND per month for motorcycles
            }

            // Calculate total price without discount
            decimal defaultTotalPrice = defaultBasePrice * durationMonths;

            // Apply discount based on duration
            int defaultDiscountPercentage = GetDiscountPercentage(durationMonths);

            // Apply discount
            decimal defaultDiscountAmount = defaultTotalPrice * defaultDiscountPercentage / 100;
            decimal defaultFinalPrice = defaultTotalPrice - defaultDiscountAmount;

            return defaultFinalPrice;
        }

        public async Task<ParkingFeeSettings> GetParkingFeeSettingsAsync()
        {
            try
            {
                if (_settingsService != null)
                {
                    // Get fee settings from database
                    return await _settingsService.GetParkingFeeSettingsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking fee settings from database. Using default values.");
            }

            // Fallback to hardcoded values if settings service is not available or fails
            return new ParkingFeeSettings
            {
                CasualMotorbikeFee = 10000,
                CasualCarFee = 30000,
                MonthlyMotorbikeFee = 100000,
                MonthlyCarFee = 500000
            };
        }

        public int GetDiscountPercentage(int durationMonths)
        {
            try
            {
                if (_settingsService != null)
                {
                    // Get discount percentage from settings
                    return _settingsService.GetDiscountPercentageAsync(durationMonths).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting discount settings from database. Using default values.");
            }

            // Fallback to hardcoded values if settings service is not available or fails
            if (durationMonths >= 3 && durationMonths < 6)
            {
                return 10; // 10% discount for 3-5 months
            }
            else if (durationMonths >= 6 && durationMonths < 12)
            {
                return 20; // 20% discount for 6-11 months
            }
            else if (durationMonths >= 12)
            {
                return 40; // 40% discount for 12+ months
            }

            return 0; // No discount for 1-2 months
        }

        public async Task<bool> CreateTestMonthlyVehicleAsync()
        {
            try
            {
                // Check if we already have test data
                var existingVehicles = await GetAllMonthlyVehiclesAsync();
                if (existingVehicles.Count > 0)
                {
                    _logger.LogInformation("Test data already exists. Skipping test data creation.");
                    return false;
                }

                // Generate a unique ID for the monthly vehicle
                string vehicleId = await _idGenerator.GenerateMonthlyVehicleId("CAR");

                // Find an available fixed parking slot
                var fixedSlot = await FindAvailableFixedSlotAsync("CAR");
                if (fixedSlot == null)
                {
                    _logger.LogError("No available fixed parking slots for test vehicle");
                    return false;
                }

                // Mark the slot as reserved
                var filter = Builders<ParkingSlot>.Filter.Eq(s => s.SlotId, fixedSlot.SlotId);
                var update = Builders<ParkingSlot>.Update.Set(s => s.Status, "RESERVED");
                await _context.ParkingSlots.UpdateOneAsync(filter, update);

                // Calculate package details
                int packageDuration = 3;
                decimal packageAmount = await CalculatePackagePrice("CAR", packageDuration);
                int discountPercentage = GetDiscountPercentage(packageDuration);

                // Calculate start and end dates
                var startDate = DateTime.Now.AddDays(-15); // Started 15 days ago
                var endDate = startDate.AddMonths(packageDuration);

                // Create a new monthly vehicle record
                var monthlyVehicle = new MonthlyVehicle
                {
                    VehicleId = vehicleId,
                    LicensePlate = "51F-12345",
                    VehicleType = "CAR",
                    CustomerName = "Test Customer",
                    CustomerPhone = "0123456789",
                    CustomerEmail = "test@example.com",
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "VALID",
                    RegistrationDate = startDate,
                    PackageDuration = packageDuration,
                    PackageAmount = packageAmount,
                    DiscountPercentage = discountPercentage,
                    FixedSlotId = fixedSlot.SlotId
                };

                // Save the monthly vehicle to the database
                await _context.MonthlyVehicles.InsertOneAsync(monthlyVehicle);
                _logger.LogInformation($"Created test monthly vehicle with ID {monthlyVehicle.Id}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test monthly vehicle");
                return false;
            }
        }
    }
}
