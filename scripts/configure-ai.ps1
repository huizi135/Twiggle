$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK and reopen PowerShell first.' }
Write-Host 'Configure the server-side OpenAI API connection. Do not paste the key into chat or commit it to Git.'
$secure = Read-Host 'OpenAI API key (hidden)' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if ([string]::IsNullOrWhiteSpace($plain)) { throw 'API key cannot be empty.' }
    dotnet user-secrets set 'AI:ApiKey' $plain --project src/Twiggle.Api
    if ($LASTEXITCODE -ne 0) { throw 'Could not save the API key.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    $plain = $null
    $secure.Dispose()
}
$model = Read-Host 'Model ID [gpt-4.1]'
if ([string]::IsNullOrWhiteSpace($model)) { $model = 'gpt-4.1' }
dotnet user-secrets set 'AI:Model' $model --project src/Twiggle.Api
if ($LASTEXITCODE -ne 0) { throw 'Could not save the model ID.' }
Write-Host 'AI configuration saved outside the repository. Restart the API and refresh the website.'
