@echo off

echo ===============================================
echo     DGIS - Publish SignerServiceCore
echo ===============================================
echo.

set ROOT=%~dp0

set PROJECT=%ROOT%SignerServiceCore\SignerServiceCore.csproj
set OUTPUT=%ROOT%InstallerFiles\SignerServiceCore

echo Project:
echo %PROJECT%
echo.

echo Output:
echo %OUTPUT%
echo.

REM -----------------------------------------------
REM Remove old publish files
REM -----------------------------------------------

if exist "%OUTPUT%" (
    echo Removing old SignerServiceCore publish...
    rmdir /S /Q "%OUTPUT%"
)

mkdir "%OUTPUT%"

echo.
echo Publishing SignerServiceCore...
echo.

dotnet publish "%PROJECT%" ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:PublishTrimmed=false ^
    -o "%OUTPUT%"

if ERRORLEVEL 1 (
    echo.
    echo ===============================================
    echo SIGNERSERVICECORE PUBLISH FAILED
    echo ===============================================
    pause
    exit /b 1
)

echo.
echo ===============================================
echo SIGNERSERVICECORE PUBLISHED SUCCESSFULLY
echo ===============================================
echo.
echo Output:
echo %OUTPUT%
echo.

pause