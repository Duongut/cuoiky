# Smart Parking System

Hệ thống quản lý bãi đỗ xe thông minh với khả năng nhận diện biển số và phân loại phương tiện. Hệ thống hỗ trợ xử lý video realtime từ webcam để tự động quản lý xe ra vào.

## Thông tin chung
- Tổng số chỗ: Có thể cấu hình qua giao diện Settings (mặc định: 250 chỗ)
  + Xe máy: Mặc định 200 chỗ (ID bắt đầu bằng M)
  + Ô tô: Mặc định 50 chỗ (ID bắt đầu bằng C)
- Đối tượng: Xe vãng lai và xe đăng ký gói tháng
- Tech stack:
  + Backend: ASP.NET Core + Flask API
  + Frontend: Vite + React
  + Database: MongoDB
  + ML Models: YOLOv5 + ML.NET
  + Video Stream: Flask + OpenCV
  + Real-time Updates: SignalR + WebSockets
  + Thanh toán: MoMo, Stripe, Tiền mặt

## Tính năng chính
- Nhận diện biển số xe tự động
- Phân loại phương tiện (ô tô/xe máy)
- Quản lý chỗ đỗ xe theo thời gian thực
- Đăng ký và gia hạn gói tháng
- Thanh toán trực tuyến qua MoMo và Stripe
- Báo cáo thống kê doanh thu
- Phân quyền người dùng (Admin/Nhân viên)
- Cấu hình hệ thống linh hoạt (giá vé, số lượng chỗ đỗ)

## Cài đặt và chạy hệ thống

### 1. Cài đặt môi trường

#### Python (License Plate Recognition API)
```bash
# Tạo và kích hoạt môi trường ảo Python
python -m venv .venv
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

# Khởi động MongoDB (Windows)
# Đảm bảo dịch vụ MongoDB đã được cài đặt và đang chạy
```

#### Frontend
```bash
# Cài đặt Node.js và npm
# https://nodejs.org/en/download/

# Cài đặt các gói phụ thuộc
cd smart-parking-frontend
npm install
```

### 2. Chuẩn bị mô hình ML

Sao chép file mô hình phân loại phương tiện:
```
SmartParking.Core\SmartParking.Core\MLModels\VehicleClassification.zip
```

vào thư mục:
```
SmartParking.Core\SmartParking.Core\bin\Debug\net8.0\MLModels\VehicleClassification.zip
```

### 3. Chạy hệ thống

#### Khởi động toàn bộ hệ thống (Khuyến nghị)
```bash
# Chạy script khởi động hệ thống
run_smart_parking.bat
```

Script này sẽ khởi động tất cả các thành phần theo thứ tự và đảm bảo chúng hoạt động đúng cách.

Để dừng hệ thống:
```bash
kill_smart_parking.bat
```

#### Hoặc khởi động từng thành phần riêng biệt:

##### Khởi động License Plate Recognition API
```bash
cd License-Plate-Recognition-main
start_api.bat
# API sẽ chạy tại http://localhost:4050
```

##### Khởi động Streaming API
```bash
cd License-Plate-Recognition-main
start_stream_api.bat
# API sẽ chạy tại http://localhost:4051
```

##### Khởi động SmartParking.Core API
```bash
cd SmartParking.Core
dotnet run --project SmartParking.Core
# API sẽ chạy tại http://localhost:5126
```

##### Khởi động Frontend
```bash
cd smart-parking-frontend
npm run dev
# Frontend sẽ chạy tại http://localhost:3000
```

## Sử dụng hệ thống

### Đăng nhập hệ thống
- Admin: thaiduonggrnff@gmail.com / password
- Nhân viên: Tạo tài khoản mới từ trang quản lý người dùng

### Các chức năng chính

#### Dashboard
- Hiển thị tổng quan về tình trạng bãi đỗ xe
- Theo dõi số lượng xe đang đỗ theo loại
- Xem danh sách xe đang đỗ

#### Check-in/Check-out
- Nhận diện biển số tự động từ hình ảnh
- Phân loại phương tiện tự động
- Cấp và giải phóng chỗ đỗ xe

#### Quản lý xe tháng
- Đăng ký gói tháng mới
- Gia hạn gói tháng
- Thanh toán trực tuyến hoặc tiền mặt
- Tự động nhận diện biển số và loại xe từ hình ảnh

#### Báo cáo
- Thống kê doanh thu theo khoảng thời gian
- Phân tích doanh thu theo loại giao dịch
- Báo cáo gói tháng và giao dịch vãng lai
- Xuất báo cáo

#### Cài đặt hệ thống
- Cấu hình giá vé và gói tháng
- Điều chỉnh số lượng chỗ đỗ xe
- Quản lý người dùng (Admin)

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
- `POST /api/vehicle/checkout/{vehicleId}`: Check-out xe ra khỏi bãi
- `GET /api/parking/slots`: Lấy danh sách tất cả các slot đỗ xe
- `GET /api/parking/vehicles/parked`: Lấy danh sách các xe đang đỗ
- `POST /api/monthlyvehicle/register`: Đăng ký xe tháng
- `POST /api/monthlyvehicle/renew/{vehicleId}`: Gia hạn xe tháng
- `GET /api/reports/transactions`: Lấy báo cáo giao dịch
- `GET /api/reports/revenue`: Lấy báo cáo doanh thu
- `GET /api/settings`: Lấy cài đặt hệ thống
- `PUT /api/settings/{key}`: Cập nhật cài đặt hệ thống

## Quy trình hoạt động

### Chế độ thủ công

1. **Xe vào bãi**:
   - Hệ thống chụp ảnh biển số
   - API nhận diện biển số và phân loại xe (ô tô/xe máy)
   - Hệ thống cấp ID và chỗ đỗ xe phù hợp
   - Lưu thông tin vào database

2. **Xe ra khỏi bãi**:
   - Hệ thống xác nhận ID xe
   - Tính toán phí đỗ xe dựa trên thời gian
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

## Cấu trúc dự án
- `License-Plate-Recognition-main/`: Mã nguồn nhận diện biển số
- `SmartParking.Core/`: API quản lý bãi đỗ xe
- `smart-parking-frontend/`: Giao diện người dùng
- `run_smart_parking.bat`: Script khởi động hệ thống
- `kill_smart_parking.bat`: Script dừng hệ thống

## Lưu ý
- Đảm bảo MongoDB đang chạy trước khi khởi động hệ thống
- Sao chép file mô hình ML.NET vào đúng vị trí như hướng dẫn
- Sử dụng run_smart_parking.bat để khởi động toàn bộ hệ thống một cách đáng tin cậy
- Hệ thống được thiết kế để chạy trên Windows