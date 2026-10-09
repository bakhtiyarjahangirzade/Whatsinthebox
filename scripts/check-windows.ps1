$ErrorActionPreference='Stop'
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows'){throw 'This script requires an isolated GitHub Windows runner. Do not run it on a working desktop.'}
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root 'artifacts\windows-checks'
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
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
 RunCheck 'shared-ui' $app @('--self-test',('"'+(Join-Path $evidence 'shared-ui.json')+'"'))
 RunCheck 'fixture' $app @('--write-test-pdf',('"'+(Join-Path $evidence 'fixture.pdf')+'"'))
 Copy-Item (Join-Path $root 'artifacts/app/*') (Join-Path $root 'artifacts/windows-tests') -Recurse -Force
 RunCheck 'direct-com' $tests @(('"'+(Join-Path $evidence 'direct-com.json')+'"'))
 RunCheck 'register' $app @('--register')
 [IO.File]::WriteAllText((Join-Path (Split-Path $app) 'test-capture.flag'),(Join-Path $evidence 'fixture.pdf'))
 RunCheck 'native-shell' $tests @(('"'+(Join-Path $evidence 'native-shell.json')+'"'),'--system-host',('"'+(Join-Path $evidence 'fixture.pdf')+'"'))
 RunCheck 'refresh-thumbnail' $app @('--refresh-thumbnail',('"'+(Join-Path $evidence 'fixture.pdf')+'"'),('"'+(Join-Path $evidence 'thumbnail-refresh.json')+'"'))
 RunCheck 'unregister' $app @('--unregister')
 $exists=Test-Path -LiteralPath 'HKCU:\Software\Classes\CLSID\{47CCD7B8-35F6-4835-965C-F488331ADE93}'
 $startup=(Get-ItemProperty -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name Whatsinthebox -ErrorAction SilentlyContinue).Whatsinthebox
 $results.Add(@{name='registration-cleanup';passed=!$exists -and !$startup})
 if($exists -or $startup){throw 'Registration cleanup failed'}
}finally {
 # Only the disposable runner's own registrations are restored here.
 Remove-Item (Join-Path (Split-Path $app) 'test-capture.flag') -ErrorAction SilentlyContinue
 try {if(Test-Path -LiteralPath $app){RunCheck 'final-cleanup' $app @('--unregister')}}
 finally {$results | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $evidence 'summary.json') -Encoding UTF8}
}
