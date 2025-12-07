@echo off
echo ========================================
echo  DevSecurityGuard - Quick Test
echo ========================================
echo.
echo Testing Multi-Package Manager Detection...
echo.

cd DevSecurityGuard.Tests

echo Running Package Manager Tests...
dotnet test --filter "FullyQualifiedName~PackageManager" --verbosity normal

echo.
echo ========================================
echo  Test Results Summary
echo ========================================
echo.

cd ..

echo.
echo Press any key to exit...
pause >nul
