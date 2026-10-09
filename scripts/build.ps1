param([switch]$Installer,[string]$RuntimeInstaller)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root 'artifacts\app'
& (Join-Path $PSScriptRoot 'build-bridge.ps1')
if(Test-Path -LiteralPath $output){
 $resolved=(Resolve-Path -LiteralPath $output).Path
 if(!$resolved.StartsWith($root+'\artifacts\') -or ((Get-Item -LiteralPath $output).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Invalid build output directory'}
 Remove-Item -LiteralPath $resolved -Recurse -Force
}
dotnet publish (Join-Path $root 'src\App\App.csproj') -c Release --self-contained true -m:1 -o $output
if($LASTEXITCODE -ne 0){throw 'Application build failed'}
dotnet publish (Join-Path $root 'src\Shell\Shell.csproj') -c Release --self-contained false -m:1 -o (Join-Path $root 'artifacts/developer-shell')
if($LASTEXITCODE -ne 0){throw 'Shell build failed'}
New-Item -ItemType Directory -Path (Join-Path $output 'shell') -Force | Out-Null
Copy-Item (Join-Path $root 'artifacts/bridge/Whatsinthebox.Bridge.dll') (Join-Path $output 'shell/Whatsinthebox.Bridge.dll') -Force
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
 ISCC (Join-Path $root 'installer\Setup.iss')
 if($LASTEXITCODE -ne 0){throw 'Installer compilation failed'}
}
Write-Output "Application: $output"
