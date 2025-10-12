# Build script for GFL Weap mod
# Run this from the mod root directory

Write-Host "Building GFL Weap mod..." -ForegroundColor Green

# Check if dotnet is available
$dotnetAvailable = Get-Command dotnet -ErrorAction SilentlyContinue

if ($dotnetAvailable) {
    Write-Host "Using dotnet to build..." -ForegroundColor Cyan
    Set-Location "Source\GFL_Weap"
    dotnet build -c Release
    Set-Location "..\..\"
} else {
    # Try MSBuild
    $msbuildPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    
    if ($msbuildPath) {
        Write-Host "Using MSBuild to build..." -ForegroundColor Cyan
        & $msbuildPath "Source\GFL_Weap\GFL_Weap.csproj" /p:Configuration=Release
    } else {
        Write-Host "ERROR: Could not find dotnet or MSBuild!" -ForegroundColor Red
        Write-Host "Please install .NET Framework 4.8 SDK or Visual Studio 2019+" -ForegroundColor Yellow
        exit 1
    }
}

if (Test-Path "Assemblies\GFL_Weap.dll") {
    Write-Host "Build successful! DLL created at Assemblies\GFL_Weap.dll" -ForegroundColor Green
    
    # Show file info
    $dllInfo = Get-Item "Assemblies\GFL_Weap.dll"
    Write-Host "File size: $($dllInfo.Length) bytes" -ForegroundColor Cyan
    Write-Host "Last modified: $($dllInfo.LastWriteTime)" -ForegroundColor Cyan
} else {
    Write-Host "ERROR: Build failed - DLL not found!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild complete! You can now use this mod in RimWorld." -ForegroundColor Green
