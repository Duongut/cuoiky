@echo off
echo Starting Smart Parking System...
setlocal EnableDelayedExpansion

REM Start License Plate Recognition API
echo Starting License Plate Recognition API...
cd License-Plate-Recognition-main
start cmd /k "title License Plate Recognition API && python api.py"
echo Waiting for License Plate Recognition API to initialize...
timeout /t 8 /nobreak > nul

REM Start Streaming API
echo Starting Streaming API...
start cmd /k "title Streaming API && python stream_api.py"
echo Waiting for Streaming API to initialize...
timeout /t 8 /nobreak > nul
cd ..

REM Start SmartParking.Core API
echo Starting SmartParking.Core API...
cd SmartParking.Core
start cmd /k "title SmartParking.Core API && dotnet run --project SmartParking.Core"
echo Waiting for SmartParking.Core API to initialize...
timeout /t 15 /nobreak > nul
cd ..

REM Wait for backend services to be ready
echo.
echo Waiting for backend services to stabilize...
echo This may take up to 30 seconds...

REM Check if License Plate Recognition API is running
set /a attempts=0
set /a max_attempts=10
:check_lpr_api
set /a attempts+=1
echo Checking License Plate Recognition API (Attempt !attempts!/!max_attempts!)...
curl -s -o nul -w "%%{http_code}" http://localhost:4050/health > temp.txt
set /p status=<temp.txt
del temp.txt
if not "!status!"=="200" (
    if !attempts! lss !max_attempts! (
        timeout /t 3 /nobreak > nul
        goto check_lpr_api
    ) else (
        echo Warning: License Plate Recognition API may not be running properly.
    )
) else (
    echo License Plate Recognition API is running.
)

REM Check if Streaming API is running
set /a attempts=0
:check_streaming_api
set /a attempts+=1
echo Checking Streaming API (Attempt !attempts!/!max_attempts!)...
curl -s -o nul -w "%%{http_code}" http://localhost:4051/health > temp.txt
set /p status=<temp.txt
del temp.txt
if not "!status!"=="200" (
    if !attempts! lss !max_attempts! (
        timeout /t 3 /nobreak > nul
        goto check_streaming_api
    ) else (
        echo Warning: Streaming API may not be running properly.
    )
) else (
    echo Streaming API is running.
)

REM Check if SmartParking.Core API is running
set /a attempts=0
:check_core_api
set /a attempts+=1
echo Checking SmartParking.Core API (Attempt !attempts!/!max_attempts!)...
curl -s -o nul -w "%%{http_code}" http://localhost:5126/swagger/index.html > temp.txt
set /p status=<temp.txt
del temp.txt
if not "!status!"=="200" (
    if !attempts! lss !max_attempts! (
        timeout /t 3 /nobreak > nul
        goto check_core_api
    ) else (
        echo Warning: SmartParking.Core API may not be running properly.
    )
) else (
    echo SmartParking.Core API is running.
)

REM Start Frontend after backend services are ready
echo.
echo All backend services are initialized. Starting Frontend...
cd smart-parking-frontend
start cmd /k "title Frontend && npm run dev"
cd ..

echo.
echo ✅ All components are now running:
echo - License Plate Recognition API: http://localhost:4050
echo - Streaming API: http://localhost:4051
echo - SmartParking.Core API: http://localhost:5126 (Swagger UI: http://localhost:5126/swagger)
echo - Frontend: http://localhost:3000
echo.
echo Open the frontend in your browser: http://localhost:3000
echo.
echo To stop all processes, run kill_smart_parking.bat
echo.

REM Wait a bit more before opening the browser to ensure frontend is ready
echo Waiting for frontend to initialize...
timeout /t 15 /nobreak > nul
echo Opening frontend in browser...
start http://localhost:3000

pause
