$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$src   = "src/SafeChm/SafeChm.csproj"
$tests = "tests/SafeChm.Tests/SafeChm.Tests.csproj"
$name  = "SafeChm"
$version = "1.0.0"
$rid   = "win-x64"

dotnet restore $src
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet restore $tests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $src -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test $tests -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dist = Join-Path $PSScriptRoot "dist"
$publishDir = Join-Path $dist "publish"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist -Force | Out-Null

dotnet publish $src -c Release -r $rid --self-contained false -o $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$zip = Join-Path $dist "$name-v$version-$rid.zip"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zip
Write-Host "Zip: $zip"
