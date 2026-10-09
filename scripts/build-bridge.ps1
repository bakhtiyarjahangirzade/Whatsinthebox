$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if(!(Test-Path $vswhere)){throw 'The native Shell bridge requires Visual Studio C++ build tools.'}
$installation=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$installation){throw 'C++ build tools not found'}
$output=Join-Path $root 'artifacts/bridge'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$environment=Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
$source=Join-Path $root 'src/Bridge/Bridge.cpp'
# Fixed compiler arguments and independently quoted paths; this script only builds.
$command='call "'+$environment+'" >nul && cl /nologo /O2 /W4 /WX /EHsc /MT /guard:cf /LD "'+$source+'" /Fo"'+$output+'\Bridge.obj" /link ole32.lib uuid.lib /DEF:"'+(Join-Path $root 'src/Bridge/Bridge.def')+'" /DYNAMICBASE /NXCOMPAT /CETCOMPAT /OUT:"'+$output+'\Whatsinthebox.Bridge.dll"'
& $env:ComSpec /d /s /c $command
if($LASTEXITCODE -ne 0){throw 'Native bridge compilation failed'}
