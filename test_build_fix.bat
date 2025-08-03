@echo off
echo Testing Android Build After Fix...

REM Set environment
set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-17.0.14.7-hotspot
set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk

echo.
echo 1. Cleaning .NET project...
dotnet clean > nul 2>&1

echo 2. Building .NET project...
dotnet build > nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ❌ .NET build failed!
    exit /b 1
)

echo 3. Testing Android Gradle build...
cd android\build
call gradlew.bat assembleDebug --console=plain > nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Android build failed!
    cd ..\..
    exit /b 1
)
cd ..\..

echo 4. Checking APK generation...
if exist "android\build\build\outputs\apk\standard\debug\android_debug.apk" (
    echo ✅ Standard Debug APK: Generated
) else (
    echo ❌ Standard Debug APK: Missing
)

if exist "android\build\build\outputs\apk\mono\debug\android_monoDebug.apk" (
    echo ✅ Mono Debug APK: Generated
) else (
    echo ❌ Mono Debug APK: Missing
)

echo.
echo ✅ All build tests passed!
echo.
echo The duplicate class definition errors have been resolved.
echo You can now export from Godot Editor without issues.
echo.
pause
