using Microsoft.ML;
using SmartParking.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SmartParking.Core.Services
{
    public class VehicleClassificationService
    {
        private readonly MLModelPrediction _mlModel;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<VehicleClassificationService> _logger;
        private readonly Dictionary<string, DateTime> _processedFrames = new();
        private readonly TimeSpan _frameProcessInterval = TimeSpan.FromMilliseconds(500); // Process every 500ms
        private readonly Dictionary<string, byte[]> _previousFrames = new(); // Store previous frames for change detection
        private readonly float _changeThreshold = 0.05f; // Minimum change required to process a new frame (5%)

        public VehicleClassificationService(
            MLModelPrediction mlModel,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<VehicleClassificationService> logger)
        {
            _mlModel = mlModel;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Phân loại phương tiện từ frame video
        /// </summary>
        /// <param name="frameBytes">Dữ liệu frame dạng byte array</param>
        /// <param name="cameraId">ID của camera</param>
        /// <param name="isWebcam">Xác định nếu frame đến từ webcam (true) hoặc upload tĩnh (false)</param>
        /// <returns>Kết quả phân loại phương tiện</returns>
        public async Task<VehicleClassificationResult> ClassifyVehicleFromFrame(byte[] frameBytes, string cameraId, bool isWebcam = true)
        {
            try
            {
                // Kiểm tra xem frame này đã được xử lý gần đây chưa
                if (isWebcam && _processedFrames.TryGetValue(cameraId, out DateTime lastProcessed))
                {
                    if (DateTime.UtcNow - lastProcessed < _frameProcessInterval)
                    {
                        // Frame đã được xử lý gần đây, bỏ qua
                        return null;
                    }
                }

                // Nếu là webcam, kiểm tra xem frame có đủ thay đổi để xử lý không
                if (isWebcam && !HasSignificantChange(frameBytes, cameraId))
                {
                    _logger.LogDebug($"Frame from camera {cameraId} has no significant change, skipping classification");
                    return null;
                }

                // Cập nhật thời gian xử lý frame
                _processedFrames[cameraId] = DateTime.UtcNow;

                // Tiền xử lý hình ảnh để cải thiện chất lượng
                byte[] processedImageBytes = await PreprocessImageAsync(frameBytes, isWebcam);

                // Lưu vào file tạm để debug nếu cần
                if (isWebcam) // Chỉ lưu file debug cho webcam frames
                {
                    string debugDir = Path.Combine(Directory.GetCurrentDirectory(), "DebugFrames");
                    if (!Directory.Exists(debugDir))
                    {
                        Directory.CreateDirectory(debugDir);
                    }
                    string tempFile = Path.Combine(debugDir, $"{cameraId}_{DateTime.Now:yyyyMMdd_HHmmss}_processed.jpg");
                    await File.WriteAllBytesAsync(tempFile, processedImageBytes);
                    _logger.LogDebug($"Saved processed image to {tempFile}");
                }

                // Sử dụng ML.NET để phân loại phương tiện trực tiếp từ byte array
                var prediction = _mlModel.PredictVehicleType(processedImageBytes);

                // Ghi log kết quả dự đoán chi tiết
                _logger.LogInformation($"Vehicle classification for camera {cameraId}: Predicted={prediction.PredictedLabel}, Confidence={prediction.GetHighestScore()}");

                // Ghi log tất cả các score
                if (prediction.Score != null && prediction.Score.Length > 0)
                {
                    _logger.LogInformation($"Classification scores: {string.Join(", ", prediction.Score)}");
                }

                // Lưu frame hiện tại để so sánh với frame tiếp theo
                if (isWebcam)
                {
                    _previousFrames[cameraId] = frameBytes;
                }

                // Trả về kết quả phân loại
                return new VehicleClassificationResult
                {
                    VehicleType = prediction.PredictedLabel,
                    Confidence = prediction.GetHighestScore(),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error classifying vehicle from frame for camera {cameraId}");
                return null;
            }
        }

        /// <summary>
        /// Lấy frame từ camera stream
        /// </summary>
        /// <param name="cameraId">ID của camera</param>
        /// <returns>Frame dạng byte array</returns>
        public async Task<byte[]> GetFrameFromCamera(string cameraId)
        {
            try
            {
                var streamingApiUrl = _configuration["StreamingAPI:BaseUrl"] ?? "http://localhost:4051";
                // Sử dụng raw-frame để lấy frame gốc không có chữ và đường viền
                var url = $"{streamingApiUrl}/cameras/{cameraId}/raw-frame";

                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }

                _logger.LogWarning($"Failed to get frame from camera {cameraId}: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting frame from camera {cameraId}");
                return null;
            }
        }



        /// <summary>
        /// Tiền xử lý hình ảnh để cải thiện chất lượng trước khi phân loại
        /// </summary>
        /// <param name="imageBytes">Dữ liệu hình ảnh gốc</param>
        /// <param name="isWebcam">Xác định nếu hình ảnh đến từ webcam</param>
        /// <returns>Dữ liệu hình ảnh đã được xử lý</returns>
        private async Task<byte[]> PreprocessImageAsync(byte[] imageBytes, bool isWebcam)
        {
            try
            {
                // Sử dụng ImageSharp để xử lý hình ảnh
                using var image = Image.Load<Rgba32>(imageBytes);

                // Áp dụng các bước xử lý khác nhau tùy thuộc vào nguồn hình ảnh
                if (isWebcam)
                {
                    // Xử lý đặc biệt cho webcam frames
                    image.Mutate(x => x
                        // Tăng độ sắc nét
                        .GaussianSharpen(0.5f)
                        // Giảm nhiễu
                        .GaussianBlur(0.3f)
                    );
                }
                else
                {
                    // Xử lý nhẹ nhàng hơn cho hình ảnh tĩnh
                    image.Mutate(x => x
                        .AutoOrient() // Tự động xoay hình ảnh theo EXIF
                    );
                }

                // Đảm bảo kích thước phù hợp với mô hình
                // Lưu ý: Điều chỉnh kích thước này phù hợp với kích thước mà mô hình ML.NET của bạn mong đợi
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(224, 224),
                    Mode = ResizeMode.Pad,
                    Position = AnchorPositionMode.Center
                }));

                // Chuyển đổi lại thành byte array
                using var memoryStream = new MemoryStream();
                await image.SaveAsJpegAsync(memoryStream, new JpegEncoder { Quality = 90 });
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preprocessing image");
                // Trả về hình ảnh gốc nếu xử lý thất bại
                return imageBytes;
            }
        }

        /// <summary>
        /// Kiểm tra xem frame hiện tại có thay đổi đáng kể so với frame trước đó không
        /// </summary>
        /// <param name="currentFrameBytes">Frame hiện tại</param>
        /// <param name="cameraId">ID của camera</param>
        /// <returns>True nếu có thay đổi đáng kể, ngược lại là False</returns>
        private bool HasSignificantChange(byte[] currentFrameBytes, string cameraId)
        {
            // Nếu không có frame trước đó, coi như có thay đổi
            if (!_previousFrames.TryGetValue(cameraId, out var previousFrameBytes))
            {
                return true;
            }

            try
            {
                // Tính toán sự khác biệt giữa hai frame
                // Phương pháp đơn giản: So sánh histogram màu
                using var currentImage = Image.Load<Rgba32>(currentFrameBytes);
                using var previousImage = Image.Load<Rgba32>(previousFrameBytes);

                // Lấy mẫu pixel để tăng tốc độ xử lý
                var currentPixels = SamplePixels(currentImage);
                var previousPixels = SamplePixels(previousImage);

                // Tính toán sự khác biệt trung bình
                float totalDifference = 0;
                for (int i = 0; i < currentPixels.Length; i++)
                {
                    var current = currentPixels[i];
                    var previous = previousPixels[i];

                    // Tính khoảng cách Euclidean giữa các giá trị màu
                    float diff = (float)Math.Sqrt(
                        Math.Pow(current.R - previous.R, 2) +
                        Math.Pow(current.G - previous.G, 2) +
                        Math.Pow(current.B - previous.B, 2));

                    // Chuẩn hóa về khoảng [0, 1]
                    totalDifference += diff / 441.67f; // sqrt(255^2 + 255^2 + 255^2)
                }

                float averageDifference = totalDifference / currentPixels.Length;
                _logger.LogDebug($"Frame difference for camera {cameraId}: {averageDifference:F4}");

                // Trả về true nếu sự khác biệt vượt quá ngưỡng
                return averageDifference > _changeThreshold;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error comparing frames for camera {cameraId}");
                // Nếu có lỗi, coi như có thay đổi để an toàn
                return true;
            }
        }

        /// <summary>
        /// Lấy mẫu pixel từ hình ảnh để tăng tốc độ xử lý
        /// </summary>
        private Rgba32[] SamplePixels(Image<Rgba32> image)
        {
            // Lấy mẫu 100 điểm trên hình ảnh (10x10 grid)
            int width = image.Width;
            int height = image.Height;
            int stepX = width / 10;
            int stepY = height / 10;

            var result = new Rgba32[100];
            int index = 0;

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    int pixelX = x * stepX + stepX / 2;
                    int pixelY = y * stepY + stepY / 2;

                    // Đảm bảo không vượt quá kích thước hình ảnh
                    pixelX = Math.Min(pixelX, width - 1);
                    pixelY = Math.Min(pixelY, height - 1);

                    result[index++] = image[pixelX, pixelY];
                }
            }

            return result;
        }

        public async Task ClassifyVehiclesRealtimeAsync(string cameraId, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting realtime vehicle classification for camera {cameraId}");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Lấy frame từ camera
                    var frameBytes = await GetFrameFromCamera(cameraId);
                    if (frameBytes == null || frameBytes.Length == 0)
                    {
                        await Task.Delay(100, cancellationToken);
                        continue;
                    }

                    // Phân loại phương tiện
                    var result = await ClassifyVehicleFromFrame(frameBytes, cameraId, true);
                    if (result != null)
                    {
                        _logger.LogInformation($"Camera {cameraId} detected vehicle: {result.VehicleType} with confidence {result.Confidence}");

                        // Ở đây bạn có thể thêm code để gửi kết quả đến client thông qua SignalR
                        // hoặc lưu vào database
                    }

                    // Đợi một khoảng thời gian trước khi xử lý frame tiếp theo
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Realtime vehicle classification for camera {cameraId} was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in realtime vehicle classification for camera {cameraId}");
            }
        }
    }

    public class VehicleClassificationResult
    {
        public string VehicleType { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
