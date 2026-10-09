$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Installer tests require an isolated Windows runner.'}
$profileKey='HKLM:\Software\Microsoft\Windows NT\CurrentVersion\ProfileList\'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$env:USERPROFILE=[Environment]::ExpandEnvironmentVariables((Get-ItemProperty -LiteralPath $profileKey).ProfileImagePath)
$env:LOCALAPPDATA=Join-Path $env:USERPROFILE 'AppData/Local'
$env:APPDATA=Join-Path $env:USERPROFILE 'AppData/Roaming'
$env:TEMP=Join-Path $env:LOCALAPPDATA 'Temp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts/windows-checks'
$installRoot=Join-Path $root 'artifacts/test-install'
$setup=(Get-ChildItem (Join-Path $root 'artifacts/installer') -Filter '*-Setup.exe' | Select-Object -First 1).FullName
$baseline=Join-Path $root 'artifacts/vendor/Whatsinthebox-0.6.0-Setup.exe'
$uninstallKey='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{1B978517-80B7-49E9-AE4F-F83A9844F190}_is1'
$thumbnailKey='HKCU:\Software\Classes\.pdf\shellex\{e357fccd-a995-4576-b01f-234630154e96}'
$sentinel='{EDCC8F6F-6881-4D14-B93D-7F50DB3AD59D}'
$results=[Collections.Generic.List[object]]::new()
$started=Get-Date
function Assert([string]$name,[bool]$passed){
 $results.Add(@{name=$name;passed=$passed})
 if(!$passed){throw "Failed: $name"}
}
function Run([string]$name,[string]$exe,[string[]]$arguments,[bool]$success=$true){
 $process=Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $handle=$process.Handle
 if(!$process.WaitForExit(180000)){Stop-Process -Id $process.Id -Force;Assert ($name+' completed') $false}
 $process.Refresh()
 Assert $name $(if($success){$process.ExitCode -eq 0}else{$process.ExitCode -ne 0})
}
function InstalledExe { (Get-ItemProperty -LiteralPath $uninstallKey).DisplayIcon.Trim('"') }
function Install([string]$name,[string]$installer,[string]$language='en',[bool]$explicitTasks=$true){
 $arguments=@('/VERYSILENT','/SUPPRESSMSGBOXES','/SP-','/NORESTART',('/LANG='+$language),('/DIR="'+$installRoot+'"'),('/LOG="'+(Join-Path $evidence ($name+'.log'))+'"'))
 if($explicitTasks){$arguments+='/TASKS=explorer'}
 Run $name $installer $arguments
 Assert ($name+' executable exists') (Test-Path -LiteralPath (InstalledExe))
}
function RemoveInstall([string]$name){
 $uninstaller=Join-Path (Get-ItemProperty -LiteralPath $uninstallKey).InstallLocation 'unins000.exe'
 Run $name $uninstaller @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/LOG="'+(Join-Path $evidence ($name+'.log'))+'"'))
 Assert ($name+' uninstall entry removed') (!(Test-Path -LiteralPath $uninstallKey))
 Assert ($name+' prior thumbnail restored') ((Get-Item -LiteralPath $thumbnailKey).GetValue('') -eq $sentinel)
 Assert ($name+' class removed') (!(Test-Path 'HKCU:\Software\Classes\CLSID\{47CCD7B8-35F6-4835-965C-F488331ADE93}'))
 $startup=(Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name Whatsinthebox -ErrorAction SilentlyContinue).Whatsinthebox
 Assert ($name+' startup removed') (!$startup)
}
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
New-Item -Path $thumbnailKey -Force | Out-Null
Set-Item -LiteralPath $thumbnailKey -Value $sentinel
try {
 foreach($language in 'en','ru','de','zh','tr'){
  Install ('clean-'+$language) $setup $language
  $exe=InstalledExe
  $locale=Get-Content (Join-Path $env:LOCALAPPDATA 'Whatsinthebox/language.json') -Raw | ConvertFrom-Json
  Assert ($language+' saved language') ($locale -eq $language)
  Run ('settings-'+$language) $exe @('--capture-settings',('"'+(Join-Path $evidence ('settings-'+$language+'.png'))+'"'),'--locale',$language)
  Assert ($language+' settings screenshot') ((Get-Item (Join-Path $evidence ('settings-'+$language+'.png'))).Length -gt 1000)
  if($language -eq 'en'){
   $before=InstalledExe
   Install 'same-version-repair' $setup
   Assert 'repair uses a fresh binary directory' ((InstalledExe) -ne $before)
   Assert 'repair restores integration' (Test-Path 'HKCU:\Software\Classes\CLSID\{47CCD7B8-35F6-4835-965C-F488331ADE93}')
   Run 'disable-integration' (InstalledExe) @('--unregister')
   Install 'disabled-repair' $setup 'en' $false
   Assert 'repair preserves disabled integration' (!(Test-Path 'HKCU:\Software\Classes\CLSID\{47CCD7B8-35F6-4835-965C-F488331ADE93}'))
  }
  RemoveInstall ('remove-'+$language)
 }
 Install 'published-baseline' $baseline
 Install 'upgrade-from-published-baseline' $setup
 $current=InstalledExe
 Assert 'upgrade selects current binary' ([Diagnostics.FileVersionInfo]::GetVersionInfo($current).FileVersion -eq [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $root 'artifacts/app/Whatsinthebox.exe')).FileVersion)
 # A real Windows executable with a newer file version supplies a read-only version-detection fixture.
 $newer=Join-Path $env:WINDIR 'System32/notepad.exe'
 Assert 'newer-version fixture' ([Diagnostics.FileVersionInfo]::GetVersionInfo($newer).FileMajorPart -gt [Diagnostics.FileVersionInfo]::GetVersionInfo($current).FileMajorPart)
 Set-ItemProperty -LiteralPath $uninstallKey -Name DisplayIcon -Value $newer
 try{Run 'downgrade-refused' $setup @('/VERYSILENT','/SUPPRESSMSGBOXES','/SP-','/NORESTART',('/DIR="'+$installRoot+'"'),('/LOG="'+(Join-Path $evidence 'downgrade.log')+'"')) $false}
 finally{Set-ItemProperty -LiteralPath $uninstallKey -Name DisplayIcon -Value $current}
 Assert 'downgrade leaves current selection unchanged' ((InstalledExe) -eq $current)
 $helpers=@(Get-CimInstance Win32_Process -Filter "Name='Whatsinthebox.exe'" | Where-Object {$_.CommandLine -like '*--windows-host*'})
 Assert 'only one startup helper' ($helpers.Count -le 1)
 RemoveInstall 'remove-upgraded-install'
 $events=@(Get-WinEvent -FilterHashtable @{LogName='Application';Id=1000,1001;StartTime=$started} -ErrorAction SilentlyContinue | Where-Object {$_.Message -match '(?i)Whatsinthebox|explorer.exe|StartMenuExperienceHost|ShellExperienceHost'})
 $events | Select-Object TimeCreated,Id,Message | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'application-events.json')
 Assert 'no application or shell crash events during installer journeys' ($events.Count -eq 0)
}finally {
 $results | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $evidence 'installer-summary.json')
 if(Test-Path $uninstallKey){
  try{Run 'emergency-unregister' (InstalledExe) @('--unregister')}catch{Write-Warning $_}
 }
}
