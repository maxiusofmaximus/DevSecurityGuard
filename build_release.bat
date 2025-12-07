@echo off
echo ===================================================
echo   Building DevSecurityGuard v1.0 Production Release
echo ===================================================

echo.
echo [1/5] Cleaning previous builds...
rmdir /s /q Publish 2>nul
mkdir Publish

echo.
echo [2/5] Publishing Launcher...
dotnet publish DevSecurityGuard.Launcher -c Release -o Publish
rem Move specific DLLs if needed, or keep flat structure but we need subfolders for components
rem We will reorganize Publish folder into clean structure

mkdir Publish\Temp
move Publish\*.* Publish\Temp\ >nul
move Publish\Temp\DevSecurityGuard.Launcher.exe Publish\
move Publish\Temp\DevSecurityGuard.Launcher.dll Publish\
move Publish\Temp\DevSecurityGuard.Launcher.runtimeconfig.json Publish\
rem Move all other dependencies to root for Launcher
move Publish\Temp\*.dll Publish\
move Publish\Temp\*.json Publish\
rmdir /s /q Publish\Temp

echo.
echo [3/5] Publishing Components (API, UI, Service)...
dotnet publish DevSecurityGuard.API -c Release -o Publish\API
dotnet publish DevSecurityGuard.UI -c Release -o Publish\UI
dotnet publish DevSecurityGuard.Service -c Release -o Publish\Service

echo.
echo [4/5] Copying Scripts & Docs...
copy install.ps1 Publish\
copy uninstall.ps1 Publish\
copy README.md Publish\
copy LICENSE Publish\
copy ARCHITECTURE.md Publish\

echo.
echo [5/5] Creating ZIP File...
powershell -Command "Compress-Archive -Path Publish\* -DestinationPath DevSecurityGuard-v1.0.0.zip -Force"

echo.
echo ===================================================
echo   SUCCESS! Package ready: DevSecurityGuard-v1.0.0.zip
echo ===================================================
pause
