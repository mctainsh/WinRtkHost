
REM Batch file to create a Windows service with delayed start

REM Set variables
set SERVICE_NAME=WinRtkHostService
set DISPLAY_NAME="WinRtkHost Service"
set DESCRIPTION="Service for WinRtkHost application"
set EXE_PATH="C:\Program Files\SecureHub\WinRtkHostService\WinRtkHost.exe"

REM Create the service
sc create %SERVICE_NAME% binPath= %EXE_PATH% DisplayName= %DISPLAY_NAME% start= delayed-auto

REM Set the service description
sc description %SERVICE_NAME% "%DESCRIPTION%"

REM Start the service
sc start %SERVICE_NAME%

echo Service %SERVICE_NAME% created and started with delayed start.
pause
