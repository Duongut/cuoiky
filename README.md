# Smart Parking System

Hệ thống quản lý bãi đỗ xe thông minh với khả năng nhận diện biển số và phân loại phương tiện. Hệ thống hỗ trợ xử lý video realtime từ webcam để tự động quản lý xe ra vào.

## Thông tin chung
- Tổng số chỗ: 250 chỗ
  + Xe máy: 200 chỗ (ID bắt đầu bằng M)
  + Ô tô: 50 chỗ (ID bắt đầu bằng C)
- Đối tượng: Xe vãng lai
- Tech stack:
  + Backend: ASP.NET Core + Flask API
  + Frontend: Vite + React
  + Database: MongoDB
  + ML Models: YOLOv5 + ML.NET
  + Video Stream: Flask + OpenCV
  + Real-time Updates: SignalR + WebSockets

## Cài đặt và chạy hệ thống

### 1. Cài đặt môi trường

#### Python (License Plate Recognition API)
```bash
# Tạo và kích hoạt môi trường ảo Python
python -m venv .venv
source .venv/bin/activate  # Linux/Mac
# hoặc
.venv\Scripts\activate  # Windows

# Cài đặt các thư viện cần thiết
cd License-Plate-Recognition-main
pip install -r requirements.txt
pip install flask

# Tải YOLOv5 (phiên bản cũ)
# Tải từ: https://drive.google.com/file/d/1g1u7M4NmWDsMGOppHocgBKjbwtDA-uIu/view?usp=sharing
# Giải nén vào thư mục License-Plate-Recognition-main/yolov5
```

#### .NET Core (SmartParking.Core API)
```bash
# Cài đặt .NET Core SDK 8.0 hoặc cao hơn
# https://dotnet.microsoft.com/download

# Khôi phục các gói NuGet
cd SmartParking.Core
dotnet restore
```

#### MongoDB
```bash
# Cài đặt MongoDB
# https://www.mongodb.com/try/download/community

# Khởi động MongoDB
mongod --dbpath=/path/to/data/db
```

### 2. Chạy hệ thống

#### Khởi động toàn bộ hệ thống
```bash
# Chạy script khởi động hệ thống
./start_system.sh
```

Hoặc khởi động từng thành phần riêng biệt:

#### Khởi động License Plate Recognition API
```bash
cd License-Plate-Recognition-main
./start_api.sh
# API sẽ chạy tại http://localhost:4050
```

#### Khởi động Streaming API
```bash
cd License-Plate-Recognition-main
./start_stream_api.sh
# API sẽ chạy tại http://localhost:4051
```

#### Khởi động SmartParking.Core API
```bash
cd SmartParking.Core
dotnet run --project SmartParking.Core
# API sẽ chạy tại https://localhost:7243 và http://localhost:5125
```

#### Khởi động Frontend
```bash
./start_frontend.sh
# Frontend sẽ chạy tại http://localhost:3000
```

#### Kiểm tra tích hợp
```bash
# Chạy script kiểm tra tích hợp
python integration_test.py

# Chạy script kiểm tra SmartParking.Core API
python test_smartparking_api.py
```

## Sử dụng hệ thống

### API Endpoints

#### License Plate Recognition API
- `GET /health`: Kiểm tra trạng thái API
- `POST /recognize`: Nhận diện biển số từ hình ảnh
  - Body: form-data với key `image` và value là file hình ảnh

#### Streaming API
- `GET /health`: Kiểm tra trạng thái API
- `GET /cameras`: Lấy danh sách các camera
- `POST /cameras/{cameraId}/start`: Bắt đầu stream từ camera
- `POST /cameras/{cameraId}/stop`: Dừng stream từ camera
- `GET /cameras/{cameraId}/stream`: Lấy video stream từ camera
- `GET /cameras/{cameraId}/detections`: Lấy các biển số được nhận diện
- `GET /cameras/{cameraId}/metrics`: Lấy metrics của camera

#### SmartParking.Core API
- `POST /api/vehicle/checkin`: Check-in xe vào bãi
  - Body: form-data với key `image` và value là file hình ảnh
- `POST /api/vehicle/checkout/{vehicleId}`: Check-out xe ra khỏi bãi
- `GET /api/parking/slots`: Lấy danh sách tất cả các slot đỗ xe
- `GET /api/parking/vehicles/parked`: Lấy danh sách các xe đang đỗ
- `GET /api/parking/initialize`: Khởi tạo các slot đỗ xe
- `GET /api/cameras`: Lấy danh sách các camera
- `POST /api/cameras/{cameraId}/start`: Bắt đầu stream từ camera
- `POST /api/cameras/{cameraId}/stop`: Dừng stream từ camera
- `GET /api/cameras/{cameraId}/detections`: Lấy các biển số được nhận diện
- `GET /api/cameras/{cameraId}/metrics`: Lấy metrics của camera

## Quy trình hoạt động

### Chế độ thủ công

1. **Xe vào bãi**:
   - Hệ thống chụp ảnh biển số
   - API nhận diện biển số và phân loại xe (ô tô/xe máy)
   - Hệ thống cấp ID và chỗ đỗ xe phù hợp
   - Lưu thông tin vào database

2. **Xe ra khỏi bãi**:
   - Hệ thống xác nhận ID xe
   - Cập nhật trạng thái xe và chỗ đỗ
   - Giải phóng chỗ đỗ xe

### Chế độ tự động (Realtime Webcam)

1. **Xe vào bãi**:
   - Camera tại cổng vào (IN-01, IN-02) ghi hình xe
   - Hệ thống tự động nhận diện biển số và phân loại xe
   - Tự động check-in xe và cấp chỗ đỗ
   - Hiển thị thông tin xe vào trên dashboard

2. **Xe ra khỏi bãi**:
   - Camera tại cổng ra (OUT-01, OUT-02) ghi hình xe
   - Hệ thống tự động nhận diện biển số
   - Tự động check-out xe và giải phóng chỗ đỗ
   - Hiển thị thông tin xe ra trên dashboard

## Phát triển

### Cấu trúc dự án
- `License-Plate-Recognition-main/`: Mã nguồn nhận diện biển số
- `SmartParking.Core/`: API quản lý bãi đỗ xe
- `integration_test.py`: Script kiểm tra tích hợp

### Các tính năng đang phát triển
- Tích hợp thanh toán
- Quản lý xe theo tháng
- Báo cáo thống kê
- Giao diện người dùng

## Đóng góp
Vui lòng liên hệ với nhóm phát triển để đóng góp vào dự án.

##### Bỏ cái file zip trong SmartParking.Core\SmartParking.Core\MLModels\VehicleClassification.zip vào trong  SmartParking.Core\SmartParking.Core\bin\Debug\net8.0\MLModels\VehicleClassification.zip