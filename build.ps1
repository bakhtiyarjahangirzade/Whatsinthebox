$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
dotnet publish App/App.csproj -c Release --self-contained true -m:1 -o dist/runtime
if ($LASTEXITCODE -ne 0) { throw 'Application build failed' }
dotnet publish Shell/Shell.csproj -c Release --self-contained false -m:1 -o dist/runtime/shell
if ($LASTEXITCODE -ne 0) { throw 'Shell build failed' }
Copy-Item licenses dist/runtime/licenses -Recurse -Force
Copy-Item LICENSE,THIRD-PARTY.md dist/runtime/ -Force
Write-Output 'Ready: dist/runtime/Whatsinthebox.exe. See README for installer compilation.'
