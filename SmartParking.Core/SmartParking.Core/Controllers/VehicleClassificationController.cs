using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SmartParking.Core.Hubs;
using SmartParking.Core.Models;
using SmartParking.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SmartParking.Core.Controllers
{
    [Route("api/vehicle-classification")]
    [ApiController]
    public class VehicleClassificationController : ControllerBase
    {
        private readonly VehicleClassificationService _vehicleClassificationService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ParkingHub> _hubContext;
        private readonly ILogger<VehicleClassificationController> _logger;
        private static readonly Dictionary<string, CancellationTokenSource> _realtimeClassificationTokens = new();

        public VehicleClassificationController(
            VehicleClassificationService vehicleClassificationService,
            IHttpClientFactory httpClientFactory,
            IHubContext<ParkingHub> hubContext,
            ILogger<VehicleClassificationController> logger)
        {
            _vehicleClassificationService = vehicleClassificationService;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("classify")]
        public async Task<IActionResult> ClassifyVehicle(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image provided");
            }

            try
            {
                // Đọc dữ liệu từ file upload
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                byte[] imageBytes = ms.ToArray();

                // Phân loại phương tiện - đây là hình ảnh tĩnh, không phải từ webcam
                var result = await _vehicleClassificationService.ClassifyVehicleFromFrame(imageBytes, "upload", false);

                if (result == null)
                {
                    return BadRequest("Failed to classify vehicle");
                }

                return Ok(new
                {
                    vehicleType = result.VehicleType,
                    confidence = result.Confidence
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying vehicle from uploaded image");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("cameras/{cameraId}/start-classification")]
        public IActionResult StartRealtimeClassification(string cameraId)
        {
            try
            {
                // Kiểm tra xem đã có phiên phân loại nào đang chạy cho camera này chưa
                if (_realtimeClassificationTokens.TryGetValue(cameraId, out var existingCts))
                {
                    return BadRequest($"Realtime classification is already running for camera {cameraId}");
                }

                // Tạo token hủy mới
                var cts = new CancellationTokenSource();
                _realtimeClassificationTokens[cameraId] = cts;

                // Bắt đầu phân loại realtime trong một task riêng biệt
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _vehicleClassificationService.ClassifyVehiclesRealtimeAsync(cameraId, cts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error in realtime classification for camera {cameraId}");
                    }
                });

                return Ok(new { message = $"Started realtime classification for camera {cameraId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting realtime classification for camera {cameraId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("cameras/{cameraId}/stop-classification")]
        public IActionResult StopRealtimeClassification(string cameraId)
        {
            try
            {
                // Kiểm tra xem có phiên phân loại nào đang chạy cho camera này không
                if (!_realtimeClassificationTokens.TryGetValue(cameraId, out var cts))
                {
                    return BadRequest($"No realtime classification running for camera {cameraId}");
                }

                // Hủy phiên phân loại
                cts.Cancel();
                _realtimeClassificationTokens.Remove(cameraId);

                return Ok(new { message = $"Stopped realtime classification for camera {cameraId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping realtime classification for camera {cameraId}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("cameras/{cameraId}/status")]
        public IActionResult GetClassificationStatus(string cameraId)
        {
            bool isRunning = _realtimeClassificationTokens.ContainsKey(cameraId);
            return Ok(new { cameraId, isClassificationRunning = isRunning });
        }
    }
}
