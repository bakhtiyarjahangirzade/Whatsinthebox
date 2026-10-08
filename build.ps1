$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet publish App/App.csproj -c Release --self-contained true -m:1 -o release-v5
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
dotnet publish Shell/Shell.csproj -c Release --self-contained false -m:1 -o release-v5/shell
if ($LASTEXITCODE -ne 0) { throw 'Shell build failed' }
Copy-Item licenses release-v5/licenses -Recurse -Force
Copy-Item LICENSE,THIRD-PARTY.md release-v5/ -Force
Write-Output 'Ready: release-v5/Whatsinthebox.exe. See README for installer compilation.'
