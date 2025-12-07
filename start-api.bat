@echo off
echo Starting DevSecurityGuard API...
echo.
echo API will be available at: http://localhost:5000
echo SignalR Hub at: http://localhost:5000/hubs/devsecurity
echo Swagger UI at: http://localhost:5000/swagger
echo.

cd DevSecurityGuard.API
dotnet run
