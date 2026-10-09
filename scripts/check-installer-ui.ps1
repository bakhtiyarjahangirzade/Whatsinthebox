$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'Installer UI tests require an isolated runner.'}
$profileKey='HKLM:\Software\Microsoft\Windows NT\CurrentVersion\ProfileList\'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$env:USERPROFILE=[Environment]::ExpandEnvironmentVariables((Get-ItemProperty -LiteralPath $profileKey).ProfileImagePath)
$env:LOCALAPPDATA=Join-Path $env:USERPROFILE 'AppData/Local'
$env:APPDATA=Join-Path $env:USERPROFILE 'AppData/Roaming'
$env:TEMP=Join-Path $env:LOCALAPPDATA 'Temp';$env:TMP=$env:TEMP
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts/windows-checks'
$setup=Join-Path $root 'artifacts/installer-ui/SetupUI.exe'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WizardCapture {
 [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out Rect rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
 [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr w,IntPtr l);
}
'@
$results=[Collections.Generic.List[object]]::new()
try {
 foreach($language in 'en','ru','de','zh','tr'){
  $state=Join-Path $evidence ('wizard-'+$language+'.txt')
  $process=Start-Process $setup -WindowStyle Hidden -PassThru -ArgumentList '/SP-','/NORESTART',('/LANG='+$language),('/DIR="'+(Join-Path $root ('artifacts/qa-'+$language))+'"'),('/QALAYOUT="'+$state+'"')
  $until=(Get-Date).AddSeconds(90)
  while(!(Test-Path $state) -and !$process.HasExited -and (Get-Date) -lt $until){Start-Sleep -Milliseconds 250}
  if(!(Test-Path $state)){throw "Wizard failed to reach finish: $language"}
  $window=Get-Process | Where-Object {$_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -like '*Whatsinthebox*'} | Select-Object -First 1
  if(!$window){throw "Installer window missing: $language"}
  $rect=[WizardCapture+Rect]::new()
  if(![WizardCapture]::GetWindowRect($window.MainWindowHandle,[ref]$rect)){throw 'Window bounds unavailable'}
  $image=[Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top)
  $graphics=[Drawing.Graphics]::FromImage($image);$dc=$graphics.GetHdc()
  try{if(![WizardCapture]::PrintWindow($window.MainWindowHandle,$dc,2)){throw 'Wizard capture failed'}}finally{$graphics.ReleaseHdc($dc);$graphics.Dispose()}
  $image.Save((Join-Path $evidence ('wizard-'+$language+'.png')));$image.Dispose()
  Stop-Process -Id $window.Id -Force
  if(!$process.WaitForExit(15000)){Stop-Process -Id $process.Id -Force;throw 'QA wizard did not close'}
  $results.Add(@{language=$language;passed=$true;state=(Get-Content $state -Raw)})
 }
}finally{$results | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $evidence 'wizard-summary.json')}
