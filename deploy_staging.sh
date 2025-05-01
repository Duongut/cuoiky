#!/bin/bash

# Smart Parking System - Staging Deployment Script
# This script prepares and deploys the system to a staging environment

echo "🚀 Starting Smart Parking System Staging Deployment"
echo "=================================================="

# Stop any running instances
echo "📋 Stopping any running instances..."
./stop_system.sh

# Build the .NET Core API
echo "🔨 Building SmartParking.Core API..."
cd SmartParking.Core/SmartParking.Core
dotnet publish -c Release -o ../../staging/api
cd ../..

# Prepare Python APIs
echo "🐍 Setting up License Plate Recognition API..."
mkdir -p staging/license-plate-api
cp -r License-Plate-Recognition-main/* staging/license-plate-api/
# Create virtual environment for Python API if it doesn't exist
if [ ! -d "staging/license-plate-api/venv" ]; then
    echo "Creating Python virtual environment..."
    cd staging/license-plate-api
    python3 -m venv venv
    source venv/bin/activate
    pip install -r requirements.txt
    deactivate
    cd ../..
fi

# Build the frontend
echo "🌐 Building frontend..."
cd smart-parking-frontend
npm install
npm run build
cd ..
mkdir -p staging/frontend
cp -r smart-parking-frontend/dist/* staging/frontend/

# Create configuration files for staging
echo "⚙️ Creating staging configuration..."
cat > staging/appsettings.staging.json << EOL
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017"
  },
  "DatabaseSettings": {
    "DatabaseName": "SmartParkingDb_Staging"
  },
  "ParkingSettings": {
    "MotorcycleSlots": 200,
    "CarSlots": 50
  },
  "LicensePlateAPI": {
    "BaseUrl": "http://localhost:4050"
  },
  "StreamingAPI": {
    "BaseUrl": "http://localhost:4051"
  },
  "ParkingFees": {
    "CasualCarFee": 30000,
    "CasualMotorbikeFee": 10000,
    "MonthlyCarFee": 300000,
    "MonthlyMotorbikeFee": 100000
  },
  "PaymentGateways": {
    "Stripe": {
      "ApiKey": "sk_test_51OvXXXXXXXXXXXXXXXXXXXXX",
      "WebhookSecret": "whsec_XXXXXXXXXXXXXXXXXXXXXXXX"
    },
    "Momo": {
      "PartnerCode": "MOMOIQA420180417",
      "AccessKey": "SvDmj2cOTYZmQQ3H",
      "SecretKey": "PPuDXq1KowPT1ftR8DvlQTHhC03aul17",
      "ApiEndpoint": "https://test-payment.momo.vn/gw_payment/transactionProcessor",
      "ReturnUrl": "http://staging.smartparking.com/payment/momo/return",
      "NotifyUrl": "http://staging.smartparking.com/api/payment/webhook/momo"
    }
  }
}
EOL

# Create start script for staging
cat > staging/start_staging.sh << EOL
#!/bin/bash

# Start MongoDB
echo "Starting MongoDB..."
mongod --fork --logpath /var/log/mongodb/mongod.log

# Start License Plate Recognition API
echo "Starting License Plate Recognition API..."
cd license-plate-api
source venv/bin/activate
nohup python api.py > ../logs/license_plate_api.log 2>&1 &
nohup python stream_api.py > ../logs/stream_api.log 2>&1 &
deactivate
cd ..

# Start SmartParking.Core API
echo "Starting SmartParking.Core API..."
cd api
nohup dotnet SmartParking.Core.dll --environment Staging > ../logs/smartparking_api.log 2>&1 &
cd ..

# Start Frontend Server
echo "Starting Frontend Server..."
cd frontend
nohup npx serve -s -l 3000 > ../logs/frontend.log 2>&1 &
cd ..

echo "✅ All components started successfully."
echo "- License Plate Recognition API: http://localhost:4050"
echo "- SmartParking.Core API: http://localhost:5125"
echo "- Frontend: http://localhost:3000"
EOL

chmod +x staging/start_staging.sh

# Create logs directory
mkdir -p staging/logs

echo "✅ Staging deployment prepared successfully!"
echo "To start the staging environment, run: cd staging && ./start_staging.sh"
