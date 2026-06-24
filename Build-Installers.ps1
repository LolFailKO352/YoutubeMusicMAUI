$ErrorActionPreference = "Stop"

Write-Host "========================================="
Write-Host " Vytvářím instalátory pro YoutubeMusic"
Write-Host "========================================="

# 1. Android
Write-Host "`n[1/2] Sestavuji Android APK..." -ForegroundColor Cyan
dotnet publish YoutubeMusic.csproj -f net10.0-android -c Release -p:AndroidPackageFormat=apk -p:RunAOTCompilation=false
if ($LASTEXITCODE -eq 0) {
    Write-Host "Android APK úspěšně vytvořeno!" -ForegroundColor Green
    $apkPath = Get-ChildItem -Path "bin\Release\net10.0-android\*Signed.apk" -Recurse | Select-Object -First 1
    if ($apkPath) {
        Write-Host "Cesta k APK: $($apkPath.FullName)"
    }
} else {
    Write-Host "Chyba při sestavování Android APK." -ForegroundColor Red
}

# 2. Windows
Write-Host "`n[2/2] Sestavuji Windows (Unpackaged + Inno Setup)..." -ForegroundColor Cyan
dotnet publish YoutubeMusic.csproj -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None -p:RuntimeIdentifierOverride=win10-x64
if ($LASTEXITCODE -eq 0) {
    Write-Host "Kompilace Windows aplikace úspěšná. Nyní generuji instalátor..." -ForegroundColor Cyan
    $isccPath = "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
    if (Test-Path $isccPath) {
        & $isccPath "YoutubeMusic.iss"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Windows instalátor (.exe) úspěšně vytvořen!" -ForegroundColor Green
            Write-Host "Cesta k instalátoru: $(Get-Location)\bin\Installers\Setup_YoutubeMusic.exe"
        } else {
            Write-Host "Chyba při tvorbě instalátoru v Inno Setup." -ForegroundColor Red
        }
    } else {
        Write-Host "Nástroj Inno Setup nebyl nalezen. Instalátor .exe nelze vytvořit." -ForegroundColor Red
    }
} else {
    Write-Host "Chyba při sestavování Windows MSIX." -ForegroundColor Red
}

Write-Host "`nHotovo!" -ForegroundColor Cyan
