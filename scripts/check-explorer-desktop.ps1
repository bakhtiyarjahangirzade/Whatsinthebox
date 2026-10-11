param([string]$Context)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts/windows-checks'
if(!$Context){
 if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Interactive checks require an isolated Windows runner.'}
 $taskName='Whatsinthebox-Explorer-'+[guid]::NewGuid().ToString('N')
 $contextFile=Join-Path $env:RUNNER_TEMP ($taskName+'.json')
 $sid=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
 icacls $root /grant ('*'+$sid+':(OI)(CI)M') /T /Q | Out-Null
 if($LASTEXITCODE -ne 0){throw 'Interactive user artifact permissions could not be granted'}
 @{authorized=$true;sid=$sid;result=(Join-Path $evidence 'desktop-task-result.json')} | ConvertTo-Json | Set-Content -LiteralPath $contextFile
 $action=New-ScheduledTaskAction -Execute (Join-Path $PSHOME 'pwsh.exe') -Argument ('-NoProfile -File "'+$PSCommandPath+'" -Context "'+$contextFile+'"')
 $principal=New-ScheduledTaskPrincipal -UserId $sid -LogonType Interactive -RunLevel Limited
 Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Settings (New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Minutes 22)) | Out-Null
 try{
  Start-ScheduledTask -TaskName $taskName
  $until=(Get-Date).AddMinutes(22)
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
$currentPrincipal=[Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
$uac=(Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System').EnableLUA
if($currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator) -and $uac -eq 1){
 try{
  Add-Type -Path (Join-Path $PSScriptRoot 'DesktopToken.cs')
  $exit=[DesktopToken]::Run((Join-Path $PSHOME 'pwsh.exe'),('-NoProfile -File "'+$PSCommandPath+'" -Context "'+$Context+'"'),$root,$authorized.sid)
  if(!(Test-Path -LiteralPath $authorized.result)){throw ('Restricted desktop worker exited '+$exit+' without a result')}
 }catch{if(!(Test-Path -LiteralPath $authorized.result)){@{success=$false;error=('Restricted token launch: '+$_.ToString())} | ConvertTo-Json | Set-Content -LiteralPath $authorized.result}}
 exit
}
$app=Join-Path $root 'artifacts/app/Whatsinthebox.exe';$tests=Join-Path $root 'artifacts/windows-tests/Whatsinthebox.Tests.exe';$fixtures=Join-Path $evidence 'desktop-fixtures'
# Output goes to files, not pipes: --register starts the long-lived Windows host, which
# inherits the caller's standard handles; a pipe would then never reach end-of-file.
function Run([string]$file,[string[]]$arguments,[int]$timeout=150){
 $script:runIndex++;$name='desktop-'+$script:runIndex+'-'+[IO.Path]::GetFileNameWithoutExtension($file)
 Add-Content -LiteralPath (Join-Path $evidence 'desktop-stage.txt') -Value ('Starting '+[IO.Path]::GetFileName($file)+' '+$arguments[0])
 $p=Start-Process -FilePath $file -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $evidence ($name+'.stdout.txt')) -RedirectStandardError (Join-Path $evidence ($name+'.stderr.txt'))
 $handle=$p.Handle
 if(!$p.WaitForExit($timeout*1000)){Stop-Process -Id $p.Id -Force;throw ('Desktop command timed out: '+$name)}
 $p.Refresh();if($p.ExitCode -ne 0){throw ('Desktop command failed '+$name+' '+$p.ExitCode)}
 Add-Content -LiteralPath (Join-Path $evidence 'desktop-stage.txt') -Value ('Completed '+$name)
}
$script:runIndex=0;$success=$false;$errorText=$null
try{
 $identity=[Security.Principal.WindowsIdentity]::GetCurrent();$principal=[Security.Principal.WindowsPrincipal]::new($identity)
 @{user=$identity.Name;session=[Diagnostics.Process]::GetCurrentProcess().SessionId;administrator=$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);uac=$uac;scenario=$(if($uac -eq 0){'interactive desktop with UAC disabled'}else{'interactive desktop with limited token'})} | ConvertTo-Json | Set-Content (Join-Path $evidence 'desktop-token.json')
 if($uac -eq 1 -and $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)){throw 'UAC-enabled desktop test must run without administrator privileges'}
 Run $app @('--register')
 Run $app @('--write-explorer-fixtures',('"'+$fixtures+'"'))
 [IO.File]::WriteAllText((Join-Path (Split-Path $app) 'shell/test-capture.flag'),'isolated runner')
 Run $tests @(('"'+(Join-Path $evidence 'actual-explorer.json')+'"'),'--explorer',('"'+$fixtures+'"')) 780
 $success=$true
}catch{$errorText=$_.ToString()}
finally{try{Run $app @('--unregister')}catch{};Remove-Item (Join-Path (Split-Path $app) 'shell/test-capture.flag') -ErrorAction SilentlyContinue;@{success=$success;error=$errorText} | ConvertTo-Json | Set-Content -LiteralPath $authorized.result}
