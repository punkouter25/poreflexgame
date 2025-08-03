@echo off
echo Setting up Android build environment...

REM Set correct JAVA_HOME
set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-17.0.14.7-hotspot
echo JAVA_HOME set to: %JAVA_HOME%

REM Set Android SDK path (you may need to adjust this based on your Android Studio installation)
if exist "C:\Users\%USERNAME%\AppData\Local\Android\Sdk" (
    set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk
    echo ANDROID_HOME set to: %ANDROID_HOME%
) else (
    echo WARNING: Android SDK not found at default location
    echo Please set ANDROID_HOME manually or install Android Studio
)

echo.
echo Environment setup complete!
echo You can now try building the Android export from Godot
pause
