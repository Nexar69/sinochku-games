$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    throw "64-bit .NET Framework C# compiler not found at $csc"
}

$args = @(
    "/nologo",
    "/codepage:65001",
    "/target:winexe",
    "/optimize+",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/win32icon:$root\assets\launcher.ico",
    "/resource:$root\assets\hero.png,Sinochku2.hero.png",
    "/resource:$root\assets\logo.png,Sinochku2.logo.png",
    "/resource:$root\assets\cover.png,Sinochku2.cover.png",
    "/out:$dist\SinochkuGames.exe",
    "$root\Program.cs"
)

& $csc @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Built $dist\SinochkuGames.exe"
