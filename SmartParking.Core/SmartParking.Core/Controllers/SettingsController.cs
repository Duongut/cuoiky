using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SmartParking.Core.Models;
using SmartParking.Core.Services;
using System;
using System.Threading.Tasks;

namespace SmartParking.Core.Controllers
{
    [Route("api/settings")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly SettingsService _settingsService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(SettingsService settingsService, ILogger<SettingsController> logger)
        {
            _settingsService = settingsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSettings()
        {
            try
            {
                var settings = await _settingsService.GetAllSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetSettingsByCategory(string category)
        {
            try
            {
                var settings = await _settingsService.GetSettingsByCategoryAsync(category);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting settings for category {category}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetSettingByKey(string key)
        {
            try
            {
                var setting = await _settingsService.GetSettingByKeyAsync(key);
                if (setting == null)
                {
                    return NotFound(new { error = $"Setting with key {key} not found" });
                }
                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting setting with key {key}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("key/{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
        {
            try
            {
                var setting = await _settingsService.UpdateSettingAsync(key, request.Value);
                return Ok(setting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating setting with key {key}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("parking-fees")]
        public async Task<IActionResult> GetParkingFeeSettings()
        {
            try
            {
                var settings = await _settingsService.GetParkingFeeSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking fee settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("parking-fees")]
        public async Task<IActionResult> UpdateParkingFeeSettings([FromBody] ParkingFeeSettings request)
        {
            try
            {
                await _settingsService.UpdateParkingFeeSettingsAsync(request);
                return Ok(new { message = "Parking fee settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parking fee settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("discounts")]
        public async Task<IActionResult> GetDiscountSettings()
        {
            try
            {
                var settings = await _settingsService.GetDiscountSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting discount settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("discounts")]
        public async Task<IActionResult> UpdateDiscountSettings([FromBody] DiscountSettings request)
        {
            try
            {
                await _settingsService.UpdateDiscountSettingsAsync(request);
                return Ok(new { message = "Discount settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating discount settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("parking-spaces")]
        public async Task<IActionResult> GetParkingSpaceSettings()
        {
            try
            {
                var settings = await _settingsService.GetParkingSpaceSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parking space settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("parking-spaces")]
        public async Task<IActionResult> UpdateParkingSpaceSettings([FromBody] ParkingSpaceSettings request)
        {
            try
            {
                await _settingsService.UpdateParkingSpaceSettingsAsync(request);
                return Ok(new { message = "Parking space settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating parking space settings");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class UpdateSettingRequest
    {
        public string Value { get; set; }
    }
}
