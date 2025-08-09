@echo off
echo Building CS2 AutoBhop...
dotnet publish -c Release
echo.
echo Build complete!
echo Output: bin\Release\net8.0-windows\win-x64\publish\AutoBhop.exe
pause