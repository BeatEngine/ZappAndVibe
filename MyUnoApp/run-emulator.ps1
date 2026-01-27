# 1. Start Emulator in the background
$emulator = "${env:ProgramFiles(x86)}\Android\android-sdk\emulator\emulator.exe"
$adb = "${env:ProgramFiles(x86)}\Android\android-sdk\platform-tools\adb.exe"

Write-Host "Starting Emulator..."
Start-Process $emulator -ArgumentList "-avd Android_Emulator_35"

# 2. Wait for ADB to recognize the device
Write-Host "Waiting for device connection..."
& $adb wait-for-device

# 3. Wait for the Android System to finish booting (critical)
Write-Host "Waiting for Android to finish booting (this can take 1-2 minutes)..."
& $adb shell 'while [[ -z $(getprop sys.boot_completed) ]]; do sleep 1; done;'

# 4. Install the APK
Write-Host "Installing APK..."
& $adb install ".\MyUnoApp\bin\Debug\net9.0-android\com.beatengine.zappnvibe-Signed.apk"

# 5. Start the App
Write-Host "Launching App..."
& $adb shell monkey -p com.beatengine.zappnvibe 1

# 6. Stream Debug Logs (Filter for common crash patterns)
Write-Host "--- LOGCAT DEBUG OUTPUT BELOW ---" -ForegroundColor Red
& $adb logcat *:E | Select-String "AndroidRuntime", "System.Exception", "Uno.UI", "Fatal"
