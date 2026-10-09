param([ValidateSet('check-windows.ps1','check-installer.ps1')][string]$Script)
$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Only isolated runners may run this launcher.'}
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts/windows-checks'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
$credential=Import-Clixml (Join-Path $env:RUNNER_TEMP 'whatsinthebox-test-user.xml')
$process=Start-Process (Join-Path $PSHOME 'pwsh.exe') -Credential $credential -LoadUserProfile -WindowStyle Hidden -PassThru -WorkingDirectory $root -ArgumentList '-NoProfile','-File',('"'+(Join-Path $PSScriptRoot $Script)+'"') -RedirectStandardOutput (Join-Path $evidence ($Script+'.stdout.txt')) -RedirectStandardError (Join-Path $evidence ($Script+'.stderr.txt'))
$handle=$process.Handle
if(!$process.WaitForExit(900000)){Stop-Process -Id $process.Id -Force;throw 'Isolated user test timed out'}
if($process.ExitCode -ne 0){Get-Content (Join-Path $evidence ($Script+'.stderr.txt'));throw "Isolated test failed ($($process.ExitCode))"}
