if exist "artifacts\1.5.1\" rmdir /s /q "artifacts\1.5.1"
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1 -Build

pause