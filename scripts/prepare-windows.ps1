$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Only isolated Windows runners may prepare this test environment.'}
$root=Split-Path $PSScriptRoot -Parent
$vendor=Join-Path $root 'artifacts/vendor'
New-Item -ItemType Directory -Path $vendor -Force | Out-Null
$metadata=Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
$runtime=$metadata.releases[0].windowsdesktop.files | Where-Object {$_.rid -eq 'win-x64' -and $_.name -like '*.exe'} | Select-Object -First 1
if(!$runtime -or ([uri]$runtime.url).Host -ne 'builds.dotnet.microsoft.com'){throw 'Unexpected runtime source'}
$path=Join-Path $vendor 'windowsdesktop-runtime-x64.exe'
Invoke-WebRequest $runtime.url -OutFile $path
if((Get-FileHash $path -Algorithm SHA512).Hash -ne $runtime.hash){throw 'Runtime checksum mismatch'}
$signature=Get-AuthenticodeSignature $path
if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation'){throw 'Runtime signature invalid'}
$process=Start-Process $path -ArgumentList '/install /quiet /norestart' -WindowStyle Hidden -PassThru
$handle=$process.Handle
if(!$process.WaitForExit(180000)){Stop-Process -Id $process.Id -Force;throw 'Runtime install timed out'}
if($process.ExitCode -notin 0,3010){throw 'Runtime install failed'}
choco install innosetup --yes --no-progress
if($LASTEXITCODE -ne 0){throw 'Inno Setup install failed'}
$compiler=Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6'
if(!(Test-Path (Join-Path $compiler 'ISCC.exe'))){throw 'Compiler missing'}
"$compiler" | Out-File $env:GITHUB_PATH -Append -Encoding utf8
$old=Join-Path $vendor 'Whatsinthebox-0.6.0-Setup.exe'
Invoke-WebRequest 'https://github.com/bakhtiyarjahangirzade/Whatsinthebox/releases/download/v0.6.0/Whatsinthebox-0.6.0-Setup.exe' -OutFile $old
if((Get-FileHash $old -Algorithm SHA256).Hash -ne '81a36efb5a0610a6c2eb5629a01cc889b5d8bf4bae88f3edbc2bcdb1ccbbbbb2'){throw 'Published baseline checksum mismatch'}
$previous=Join-Path $vendor 'Whatsinthebox-1.0.0-Setup.exe'
Invoke-WebRequest 'https://github.com/bakhtiyarjahangirzade/Whatsinthebox/releases/download/v1.0.0/Whatsinthebox-1.0.0-Setup.exe' -OutFile $previous
if((Get-FileHash $previous -Algorithm SHA256).Hash -ne 'a4be900f44ea040bcd3a0963a88a2a3867891d9677d4c731d716084145bcb305'){throw 'Previous stable installer checksum mismatch'}
# The runner administrator token is unsuitable for per-user Shell registrations.
# Exercise the application as an ordinary user, as the per-user installer does.
$name='WhatsintheboxTest'
$password=ConvertTo-SecureString ('Witb!'+[guid]::NewGuid().ToString('N')+'aA9') -AsPlainText -Force
New-LocalUser -Name $name -Password $password -AccountNeverExpires -PasswordNeverExpires | Out-Null
Add-LocalGroupMember -SID 'S-1-5-32-545' -Member $name
$credential=[pscredential]::new(($env:COMPUTERNAME+'\'+$name),$password)
$credential | Export-Clixml (Join-Path $env:RUNNER_TEMP 'whatsinthebox-test-user.xml')
icacls $root /grant ($name+':(OI)(CI)M') /T /Q | Out-Null
if($LASTEXITCODE -ne 0){throw 'Test directory access could not be granted'}
