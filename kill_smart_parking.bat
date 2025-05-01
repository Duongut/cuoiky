@echo off
echo Stopping Smart Parking System processes...

REM Kill Python processes (API and Streaming API)
echo Stopping Python processes...
taskkill /F /FI "WINDOWTITLE eq License Plate Recognition API" /T
taskkill /F /FI "WINDOWTITLE eq Streaming API" /T

REM Kill .NET process (SmartParking.Core API)
echo Stopping .NET process...
taskkill /F /FI "WINDOWTITLE eq SmartParking.Core API" /T

REM Kill Node.js process (Frontend)
echo Stopping Frontend...
taskkill /F /FI "WINDOWTITLE eq Frontend" /T

echo.
echo All Smart Parking System processes have been terminated.
echo.

pause
