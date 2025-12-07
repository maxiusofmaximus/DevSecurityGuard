@echo off
echo ========================================
echo  DevSecurityGuard - Complete System Test
echo ========================================
echo.

echo [1/5] Building all projects...
dotnet build --configuration Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo [2/5] Starting API Server...
start "DevSecurityGuard API" cmd /k "cd DevSecurityGuard.API && dotnet run"
timeout /t 5 /nobreak >nul

echo.
echo [3/5] Opening Web UI...
start "" "DevSecurityGuard.Web\index.html"
timeout /t 2 /nobreak >nul

echo.
echo [4/5] Opening Architecture View...
start "" "DevSecurityGuard.Web\architecture.html"
timeout /t 2 /nobreak >nul

echo.
echo [5/5] System Status Check...
echo.
echo ✅ API Running on: http://localhost:5000
echo ✅ Web UI: file:///%CD%/DevSecurityGuard.Web/index.html
echo ✅ Architecture: file:///%CD%/DevSecurityGuard.Web/architecture.html
echo ✅ SignalR Hub: http://localhost:5000/hubs/devsecurity
echo.
echo ========================================
echo  System Ready! Check the opened windows
echo ========================================
echo.
echo Press any key to stop all services...
pause >nul

echo.
echo Stopping services...
taskkill /FI "WINDOWTITLE eq DevSecurityGuard API*" /F >nul 2>&1

echo.
echo All services stopped.
pause
