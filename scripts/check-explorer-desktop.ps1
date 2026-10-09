param([string]$Context)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts/windows-checks'
if(!$Context){
 if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Interactive checks require an isolated Windows runner.'}
 $taskName='Whatsinthebox-Explorer-'+[guid]::NewGuid().ToString('N')
 $contextFile=Join-Path $env:RUNNER_TEMP ($taskName+'.json')
 $sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
 @{authorized=$true;sid=$sid;result=(Join-Path $evidence 'desktop-task-result.json')} | ConvertTo-Json | Set-Content -LiteralPath $contextFile
 $action=New-ScheduledTaskAction -Execute (Join-Path $PSHOME 'pwsh.exe') -Argument ('-NoProfile -File "'+$PSCommandPath+'" -Context "'+$contextFile+'"')
 $principal=New-ScheduledTaskPrincipal -UserId $sid -LogonType Interactive -RunLevel Limited
 Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 5)) | Out-Null
 try{
  Start-ScheduledTask -TaskName $taskName
  $until=(Get-Date).AddMinutes(5)
  $resultFile=Join-Path $evidence 'desktop-task-result.json'
  while(!(Test-Path -LiteralPath $resultFile) -and (Get-Date) -lt $until){Start-Sleep -Seconds 5}
  if(!(Test-Path -LiteralPath $resultFile)){throw 'Interactive desktop task did not finish.'}
  $result=Get-Content -LiteralPath $resultFile -Raw | ConvertFrom-Json
  if(!$result.success){throw ('Interactive Explorer check failed: '+$result.error)}
 }finally{Stop-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue;Unregister-ScheduledTask -TaskName $taskName -Confirm:$false;Remove-Item -LiteralPath $contextFile -Force}
 exit
}
$authorized=Get-Content -LiteralPath $Context -Raw | ConvertFrom-Json
if(!$authorized.authorized -or $authorized.sid -ne [Security.Principal.WindowsIdentity]::GetCurrent().User.Value){throw 'Invalid isolated desktop context'}
$env:GITHUB_ACTIONS='true';$env:RUNNER_OS='Windows'
$app=Join-Path $root 'artifacts/app/Whatsinthebox.exe';$tests=Join-Path $root 'artifacts/windows-tests/Whatsinthebox.Tests.exe';$fixtures=Join-Path $evidence 'desktop-fixtures'
function Run([string]$file,[string[]]$arguments){$p=Start-Process -FilePath $file -ArgumentList $arguments -PassThru;$handle=$p.Handle;if(!$p.WaitForExit(150000)){Stop-Process -Id $p.Id -Force;throw 'Desktop command timed out'};if($p.ExitCode -ne 0){throw ('Desktop command failed '+$p.ExitCode)}}
$success=$false;$errorText=$null
try{
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
 @{user=$identity.Name;session=[Diagnostics.Process]::GetCurrentProcess().SessionId;administrator=$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)} | ConvertTo-Json | Set-Content (Join-Path $evidence 'desktop-token.json')
 if($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'Desktop test must run without administrator privileges'}
 Run $app @('--register')
 Run $app @('--write-explorer-fixtures',('"'+$fixtures+'"'))
 [IO.File]::WriteAllText((Join-Path (Split-Path $app) 'test-capture.flag'),'*')
 Run $tests @(('"'+(Join-Path $evidence 'actual-explorer.json')+'"'),'--explorer',('"'+$fixtures+'"'))
 $success=$true
}catch{$errorText=$_.ToString()}
finally{try{Run $app @('--unregister')}catch{};Remove-Item (Join-Path (Split-Path $app) 'test-capture.flag') -ErrorAction SilentlyContinue;@{success=$success;error=$errorText} | ConvertTo-Json | Set-Content -LiteralPath $authorized.result}
