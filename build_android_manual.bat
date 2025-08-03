@echo off
echo Starting Android Build Process...

REM Set environment variables
set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-17.0.14.7-hotspot
set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk

echo Environment Variables Set:
echo JAVA_HOME: %JAVA_HOME%
echo ANDROID_HOME: %ANDROID_HOME%

echo.
echo Cleaning previous builds...
if exist "builds\android\*.apk" del /Q "builds\android\*.apk"
if exist "android\build\build\outputs\apk" rmdir /S /Q "android\build\build\outputs\apk"

echo.
echo Testing Gradle build system...
cd android\build
call gradlew.bat assembleDebug --console=plain --stacktrace

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Gradle build successful!
    echo.
    echo APK files generated:
    if exist "build\outputs\apk\standard\debug\android_debug.apk" (
        echo   - Standard Debug APK: android\build\build\outputs\apk\standard\debug\android_debug.apk
        copy "build\outputs\apk\standard\debug\android_debug.apk" "..\..\builds\android\PoReflexGame-debug.apk"
        echo   - Copied to: builds\android\PoReflexGame-debug.apk
    )
    if exist "build\outputs\apk\mono\debug\android_monoDebug.apk" (
        echo   - Mono Debug APK: android\build\build\outputs\apk\mono\debug\android_monoDebug.apk
        copy "build\outputs\apk\mono\debug\android_monoDebug.apk" "..\..\builds\android\PoReflexGame-mono-debug.apk"
        echo   - Copied to: builds\android\PoReflexGame-mono-debug.apk
    )
    echo.
    echo ✅ Android build process completed successfully!
    echo You can now try exporting from Godot Editor.
) else (
    echo.
    echo ❌ Gradle build failed with error code %ERRORLEVEL%
    echo Check the output above for specific error details.
    echo.
    echo Common solutions:
    echo 1. Ensure Android Studio is installed with SDK tools
    echo 2. Accept Android SDK licenses in Android Studio
    echo 3. Check that NDK is installed
)

cd ..\..
echo.
pause
