$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'This script requires an isolated GitHub Windows runner. Do not run it on a working desktop.'}
$profileKey='HKLM:\Software\Microsoft\Windows NT\CurrentVersion\ProfileList\'+[Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$env:USERPROFILE=[Environment]::ExpandEnvironmentVariables((Get-ItemProperty -LiteralPath $profileKey).ProfileImagePath)
$env:LOCALAPPDATA=Join-Path $env:USERPROFILE 'AppData/Local'
$env:APPDATA=Join-Path $env:USERPROFILE 'AppData/Roaming'
$env:TEMP=Join-Path $env:LOCALAPPDATA 'Temp'
$env:TMP=$env:TEMP
New-Item -ItemType Directory -Path $env:TEMP -Force | Out-Null
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts\windows-checks'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
whoami /groups | Set-Content (Join-Path $evidence 'runner-token.txt')
$app=Join-Path $root 'artifacts\app\Whatsinthebox.exe'
$tests=Join-Path $root 'artifacts\windows-tests\Whatsinthebox.Tests.exe'
$results=[Collections.Generic.List[object]]::new()
function RunCheck([string]$name,[string]$file,[string[]]$arguments,[int]$timeout=120){
 $stdout=Join-Path $evidence ($name+'.stdout.txt')
 $stderr=Join-Path $evidence ($name+'.stderr.txt')
 $process=Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
 $handle=$process.Handle
 $completed=$process.WaitForExit($timeout*1000)
 if(!$completed){Stop-Process -Id $process.Id -Force;$results.Add(@{name=$name;passed=$false;timeout=$true});throw "Timed out: $name"}
 $process.Refresh()
 $results.Add(@{name=$name;passed=$process.ExitCode -eq 0;exit=$process.ExitCode})
 if($process.ExitCode -ne 0){throw "Failed: $name ($($process.ExitCode))"}
}
try {
 $savedRoot=$env:DOTNET_ROOT;$savedX64=$env:DOTNET_ROOT_X64;$savedLookup=$env:DOTNET_MULTILEVEL_LOOKUP
 try{
  $env:DOTNET_ROOT=Join-Path $root 'artifacts/no-global-runtime';$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT;$env:DOTNET_MULTILEVEL_LOOKUP='0'
  RunCheck 'self-contained-runtime' $app @('--verify-rendering',('"'+(Join-Path $evidence 'self-contained-runtime.json')+'"'))
 }finally{$env:DOTNET_ROOT=$savedRoot;$env:DOTNET_ROOT_X64=$savedX64;$env:DOTNET_MULTILEVEL_LOOKUP=$savedLookup}
 try{RunCheck 'shared-ui' $app @('--self-test',('"'+(Join-Path $evidence 'shared-ui.json')+'"'))}catch{Write-Warning $_}
 RunCheck 'fixture' $app @('--write-test-pdf',('"'+(Join-Path $evidence 'fixture.pdf')+'"'))
 Copy-Item (Join-Path $root 'artifacts/app/*') (Join-Path $root 'artifacts/windows-tests') -Recurse -Force
 Copy-Item (Join-Path $root 'artifacts/developer-shell/*') (Join-Path $root 'artifacts/windows-tests/shell') -Recurse -Force
 RunCheck 'direct-com' $tests @(('"'+(Join-Path $evidence 'direct-com.json')+'"'))
 RunCheck 'register' $app @('--register')
 [IO.File]::WriteAllText((Join-Path (Split-Path $app) 'test-capture.flag'),(Join-Path $evidence 'fixture.pdf'))
 RunCheck 'native-shell' $tests @(('"'+(Join-Path $evidence 'native-shell.json')+'"'),'--system-host',('"'+(Join-Path $evidence 'fixture.pdf')+'"'))
 RunCheck 'refresh-thumbnail' $app @('--refresh-thumbnail',('"'+(Join-Path $evidence 'fixture.pdf')+'"'),('"'+(Join-Path $evidence 'thumbnail-refresh.json')+'"'))
 foreach($iteration in 1..3){RunCheck ('repeat-shell-'+$iteration) $tests @(('"'+(Join-Path $evidence ('repeat-shell-'+$iteration+'.json'))+'"'),'--system-host',('"'+(Join-Path $evidence 'fixture.pdf')+'"'))}
 RunCheck 'unregister' $app @('--unregister')
 $exists=Test-Path -LiteralPath 'HKCU:\Software\Classes\CLSID\{916D5157-F38C-4068-A7E9-613E8E6DFD64}'
 $startup=(Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name Whatsinthebox -ErrorAction SilentlyContinue).Whatsinthebox
 $results.Add(@{name='registration-cleanup';passed=!$exists -and !$startup})
 if($exists -or $startup){throw 'Registration cleanup failed'}
 if(@($results | Where-Object {!$_.passed}).Count){throw 'One or more Windows checks failed; see summary.json'}
}finally {
 # Only the disposable runner's own registrations are restored here.
 Remove-Item (Join-Path (Split-Path $app) 'test-capture.flag') -ErrorAction SilentlyContinue
 try {if(Test-Path -LiteralPath $app){RunCheck 'final-cleanup' $app @('--unregister')}}
 finally {$results | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $evidence 'summary.json') -Encoding UTF8}
}
