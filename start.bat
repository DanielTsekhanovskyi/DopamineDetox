@echo off
echo ==============================================
echo      Dopamine Detox Tracker - Startup
echo ==============================================

echo [1/3] Starting Backend API...
start "Backend API" cmd /c "cd Tracker.Api && dotnet run"

echo [2/3] Starting Desktop Agent...
start "Desktop Agent" cmd /c "cd Tracker.DesktopAgent && dotnet run"

echo [3/3] Starting Frontend Dashboard...
start "React Dashboard" cmd /c "cd tracker-client && npm run dev"

echo ==============================================
echo All services started in separate windows!
echo The dashboard will open in your browser automatically.
echo Close this window when you are done.
echo ==============================================
pause