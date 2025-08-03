# Android Export Quick Fix Guide

## Immediate Actions Required

### 1. Set Environment Variables
Run the provided script:
```batch
fix_android_env.bat
```

### 2. In Godot Editor
1. **Open Project → Export**
2. **Verify Android preset exists** (should now be available)
3. **Check export settings**:
   - Export Path: `builds/android/PoReflexGame.apk`
   - Package Name: `com.poreflexgame.app`
   - Min SDK: 24, Target SDK: 34

### 3. Install Export Templates (if needed)
If you see "Export templates not found":
1. Go to **Editor → Manage Export Templates**
2. Download templates for Godot 4.4
3. Or download from: https://godotengine.org/download/

### 4. Android SDK Setup (if needed)
If Android SDK path is not set:
1. **Editor → Editor Settings → Export → Android**
2. Set **Android SDK Path** to your Android Studio SDK location
   - Usually: `C:\Users\[username]\AppData\Local\Android\Sdk`
3. Set **Debug Keystore** to the existing `debug.keystore` file

## Common Export Paths

### For Debug Build:
```
Project → Export → Select Android → Export Project
```

### For Release Build:
1. Ensure you have a release keystore
2. Configure signing in export preset
3. Export with release configuration

## Troubleshooting

### If Gradle Still Fails:
1. Check Android Studio is installed with SDK tools
2. Verify NDK is installed (version 23.2.8568313)
3. Run Android Studio once to accept licenses

### If APK Won't Install:
- Enable "Unknown Sources" on Android device
- Use `adb install` for debugging
- Check minimum Android version (API 24 = Android 7.0)

## Testing the Fix
After following the steps above, try exporting. The error should be resolved and you should get a successful APK build.
