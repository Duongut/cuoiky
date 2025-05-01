@echo off
echo Installing JWT Bearer package...

cd SmartParking.Core
dotnet add SmartParking.Core package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0

echo Package installed successfully.
pause
