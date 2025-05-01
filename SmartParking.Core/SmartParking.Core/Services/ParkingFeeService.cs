using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartParking.Core.Models;
using System;
using System.Threading.Tasks;

namespace SmartParking.Core.Services
{
    public class ParkingFeeService
    {
        private readonly IConfiguration _configuration;
        private readonly SettingsService _settingsService;
        private readonly ILogger<ParkingFeeService> _logger;

        public ParkingFeeService(IConfiguration configuration, SettingsService settingsService, ILogger<ParkingFeeService> logger)
        {
            _configuration = configuration;
            _settingsService = settingsService;
            _logger = logger;
        }

        /// <summary>
        /// Calculate parking fee based on vehicle type using fixed fee structure
        /// </summary>
        /// <param name="vehicleType">Type of vehicle (CAR or MOTORBIKE)</param>
        /// <param name="entryTime">Time when vehicle entered</param>
        /// <param name="exitTime">Time when vehicle exited</param>
        /// <param name="isMonthlyRegistered">Whether the vehicle is registered for monthly parking</param>
        /// <returns>Calculated fee in VND</returns>
        public decimal CalculateParkingFee(string vehicleType, DateTime entryTime, DateTime exitTime, bool isMonthlyRegistered = false)
        {
            // If the vehicle is registered for monthly parking, no fee is charged
            if (isMonthlyRegistered)
            {
                return 0;
            }

            try
            {
                // Get fee settings from database
                var feeSettings = _settingsService.GetParkingFeeSettingsAsync().GetAwaiter().GetResult();

                // Get fixed rates for casual parking
                decimal casualCarFee = feeSettings.CasualCarFee;
                decimal casualMotorbikeFee = feeSettings.CasualMotorbikeFee;

                // Calculate fee based on vehicle type (fixed fee per parking session)
                decimal fee = vehicleType.ToUpper() == "CAR" ? casualCarFee : casualMotorbikeFee;

                return fee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking fee settings from database. Using default values from configuration.");

                // Fallback to configuration if database settings are not available
                var feeConfig = _configuration.GetSection("ParkingFees");
                decimal casualCarFee = feeConfig.GetValue<decimal>("CasualCarFee", 30000);
                decimal casualMotorbikeFee = feeConfig.GetValue<decimal>("CasualMotorbikeFee", 10000);
                decimal fee = vehicleType.ToUpper() == "CAR" ? casualCarFee : casualMotorbikeFee;

                return fee;
            }
        }



        /// <summary>
        /// Format parking duration as a human-readable string
        /// </summary>
        /// <param name="entryTime">Time when vehicle entered</param>
        /// <param name="exitTime">Time when vehicle exited</param>
        /// <returns>Formatted duration string</returns>
        public string FormatParkingDuration(DateTime entryTime, DateTime exitTime)
        {
            TimeSpan duration = exitTime - entryTime;

            if (duration.TotalDays >= 1)
            {
                return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
            }
            else if (duration.TotalHours >= 1)
            {
                return $"{duration.Hours}h {duration.Minutes}m";
            }
            else
            {
                return $"{duration.Minutes}m {duration.Seconds}s";
            }
        }
    }
}
