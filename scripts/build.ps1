param([switch]$Installer,[string]$RuntimeInstaller)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root 'artifacts\app'
if(Test-Path -LiteralPath $output){
 $resolved=(Resolve-Path -LiteralPath $output).Path
 if(!$resolved.StartsWith($root+'\artifacts\') -or ((Get-Item -LiteralPath $output).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid build output directory'}
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
dotnet publish (Join-Path $root 'src\App\App.csproj') -c Release --self-contained true -m:1 -o $output
if($LASTEXITCODE -ne 0){throw 'Application build failed'}
dotnet publish (Join-Path $root 'src\Shell\Shell.csproj') -c Release --self-contained false -m:1 -o (Join-Path $output 'shell')
if($LASTEXITCODE -ne 0){throw 'Shell build failed'}
foreach($symbol in (Get-ChildItem -LiteralPath $output -Filter '*.pdb' -Recurse -File)){
 if(!$symbol.FullName.StartsWith($output+'\')){throw 'Invalid symbol path'}
 Remove-Item -LiteralPath $symbol.FullName -Force
}
$notice=Get-Content (Join-Path $root 'NOTICE.txt') -Raw
foreach($name in 'LICENSE.txt','ThirdPartyNotices.txt'){
 $path=Join-Path $output $name
 if(Test-Path -LiteralPath $path){$notice+="`r`n===== Microsoft .NET: $name =====`r`n"+(Get-Content -LiteralPath $path -Raw)}
}
[IO.File]::WriteAllText((Join-Path $output 'NOTICE.txt'),$notice,[Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $output 'LICENSE.txt') -Force
if(Test-Path -LiteralPath (Join-Path $output 'ThirdPartyNotices.txt')){Remove-Item -LiteralPath (Join-Path $output 'ThirdPartyNotices.txt')}
if($Installer){
 if(!$RuntimeInstaller -or !(Test-Path -LiteralPath $RuntimeInstaller)){throw 'Pass -RuntimeInstaller with the official Microsoft .NET Desktop Runtime x64 installer.'}
 $signature=Get-AuthenticodeSignature -LiteralPath $RuntimeInstaller
 if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation'){throw 'Microsoft runtime signature is invalid'}
 $vendor=Join-Path $root 'artifacts\vendor'
 New-Item -ItemType Directory -Path $vendor -Force | Out-Null
 Copy-Item -LiteralPath $RuntimeInstaller -Destination (Join-Path $vendor 'windowsdesktop-runtime-x64.exe') -Force
 ISCC (Join-Path $root 'installer\Setup.iss')
 if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
}
Write-Output "Application: $output"
