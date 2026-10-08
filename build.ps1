$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet publish App/App.csproj -c Release --self-contained true -o release-v4
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
dotnet publish Shell/Shell.csproj -c Release --self-contained false -o release-v4/shell
if ($LASTEXITCODE -ne 0) { throw 'Shell build failed' }
Copy-Item licenses release-v4/licenses -Recurse -Force
Copy-Item LICENSE,THIRD-PARTY.md release-v4/ -Force
Write-Output 'Ready: release-v4/Whatsinthebox.exe. See README for installer compilation.'
