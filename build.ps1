<#
.SYNOPSIS
    Builds FocusPulse for x86, x64, and ARM64 / AnyCPU architectures.

.DESCRIPTION
    Uses the native Windows C# compiler (csc.exe) to generate ultra-lightweight, 
    standalone executables without requiring Visual Studio or external package managers.
#>

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "         FocusPulse - Multi-Architecture Build            " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$baseDir = $PSScriptRoot
$srcDir  = Join-Path $baseDir "src"
$binDir  = Join-Path $baseDir "bin"

if (-not (Test-Path $binDir)) {
    New-Item -ItemType Directory -Path $binDir | Out-Null
}

$csc64 = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$csc32 = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if (-not (Test-Path $csc64)) {
    Write-Error ".NET Framework 64-bit compiler not found at $csc64"
    exit 1
}

$sources = @(
    (Join-Path $srcDir "NativeMethods.cs"),
    (Join-Path $srcDir "FocusPulseModels.cs"),
    (Join-Path $srcDir "FocusPulseStyles.cs"),
    (Join-Path $srcDir "ActivityEngine.cs"),
    (Join-Path $srcDir "SyncClient.cs"),
    (Join-Path $srcDir "MainWindow.cs"),
    (Join-Path $srcDir "App.cs")
)

$manifest = (Join-Path $srcDir "app.manifest")

$refs64 = @(
    "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationFramework.dll",
    "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\PresentationCore.dll",
    "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll",
    "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll",
    "/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Web.Extensions.dll",
    "/r:System.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll"
)

$refs32 = @(
    "/r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationFramework.dll",
    "/r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF\PresentationCore.dll",
    "/r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF\WindowsBase.dll",
    "/r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Xaml.dll",
    "/r:C:\Windows\Microsoft.NET\Framework\v4.0.30319\System.Web.Extensions.dll",
    "/r:System.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll"
)

# Clean legacy binaries
Remove-Item (Join-Path $binDir "*ActivityWatch*.exe") -ErrorAction SilentlyContinue

$iconPath = Join-Path $srcDir "FocusPulse.ico"
$iconArg = if (Test-Path $iconPath) { 
    Copy-Item $iconPath (Join-Path $binDir "FocusPulse.ico") -Force -ErrorAction SilentlyContinue
    "/win32icon:$iconPath" 
} else { "" }

# 1. Build x64 (AMD64 / Intel 64-bit)
Write-Host "`n[1/3] Building target: x64 (bin\FocusPulse-x64.exe)..." -ForegroundColor Yellow
$outX64 = Join-Path $binDir "FocusPulse-x64.exe"
$argsX64 = @("/target:winexe", "/platform:x64", "/optimize+", "/win32manifest:$manifest")
if ($iconArg) { $argsX64 += $iconArg }
$argsX64 += @("/out:$outX64") + $refs64 + $sources
& $csc64 $argsX64
if ($LASTEXITCODE -eq 0) {
    Write-Host "  -> Success: $(Get-Item $outX64 | ForEach-Object { "$($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)" })" -ForegroundColor Green
} else {
    Write-Error "x64 build failed with exit code $LASTEXITCODE"
}

# 2. Build x86 (Intel/AMD 32-bit)
Write-Host "`n[2/3] Building target: x86 (bin\FocusPulse-x86.exe)..." -ForegroundColor Yellow
$outX86 = Join-Path $binDir "FocusPulse-x86.exe"
$argsX86 = @("/target:winexe", "/platform:x86", "/optimize+", "/win32manifest:$manifest")
if ($iconArg) { $argsX86 += $iconArg }
$argsX86 += @("/out:$outX86") + $refs32 + $sources
& $csc32 $argsX86
if ($LASTEXITCODE -eq 0) {
    Write-Host "  -> Success: $(Get-Item $outX86 | ForEach-Object { "$($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)" })" -ForegroundColor Green
} else {
    Write-Error "x86 build failed with exit code $LASTEXITCODE"
}

# 3. Build ARM64 / Universal AnyCPU (Runs natively on ARM64 / Snapdragon X & Surface Pro)
Write-Host "`n[3/3] Building target: ARM64 / Universal (bin\FocusPulse-ARM64.exe)..." -ForegroundColor Yellow
$outArm = Join-Path $binDir "FocusPulse-ARM64.exe"
$argsArm = @("/target:winexe", "/platform:anycpu", "/optimize+", "/win32manifest:$manifest")
if ($iconArg) { $argsArm += $iconArg }
$argsArm += @("/out:$outArm") + $refs64 + $sources
& $csc64 $argsArm
if ($LASTEXITCODE -eq 0) {
    Write-Host "  -> Success: $(Get-Item $outArm | ForEach-Object { "$($_.Name) ($([math]::Round($_.Length/1KB, 1)) KB)" })" -ForegroundColor Green
} else {
    Write-Error "ARM64 build failed with exit code $LASTEXITCODE"
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "   All 3 architectures built successfully in .\bin\       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan
