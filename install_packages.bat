@echo off
echo Installing required NuGet packages...

cd SmartParking.Core
dotnet add SmartParking.Core package BCrypt.Net-Next
dotnet add SmartParking.Core package Microsoft.AspNetCore.Authentication.JwtBearer

echo Packages installed successfully.
pause
