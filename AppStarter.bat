@echo off

:: Store the current directory (assumes the script is in the same folder as the .sln file)
set "SOLUTION_DIR=%~dp0"

:: Paths to the projects relative to the solution directory
set "CLIENT_DIR=%SOLUTION_DIR%BroadcastClient"
set "SERVER_DIR=%SOLUTION_DIR%BroadcastServer"

:: Start the BroadcastClient
cd /d "%CLIENT_DIR%"
start cmd /k "dotnet run"

:: Start the BroadcastServer
cd /d "%SERVER_DIR%"
start cmd /k "dotnet run"

:: Go back to the solution directory (optional)
cd /d "%SOLUTION_DIR%"
