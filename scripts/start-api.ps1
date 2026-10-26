$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (!(Test-Path '.env')) { throw 'Copy .env.example to .env and set TWIGGLE_DB_PASSWORD first.' }
$line = Get-Content '.env' | Where-Object { $_ -match '^TWIGGLE_DB_PASSWORD=' } | Select-Object -First 1
if (!$line) { throw 'TWIGGLE_DB_PASSWORD is missing from .env.' }
$password = $line.Substring('TWIGGLE_DB_PASSWORD='.Length).Trim()
if (!$password -or $password -eq 'replace-with-a-local-password' -or $password -match '[;"\r\n]') { throw 'Use a local password without semicolons or quotes.' }
$env:ConnectionStrings__Twiggle = "Host=127.0.0.1;Port=54329;Database=twiggle;Username=twiggle;Password=$password;Timeout=3"
try {
    dotnet run --project src/Twiggle.Api
    if ($LASTEXITCODE -ne 0) { throw "API exited with code $LASTEXITCODE" }
} finally { Remove-Item Env:ConnectionStrings__Twiggle -ErrorAction SilentlyContinue }
