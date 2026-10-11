$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
if(!(Test-Path $vswhere)){throw 'The native Explorer handlers require Visual Studio C++ build tools.'}
$installation=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$installation){throw 'C++ build tools not found'}
$source=Join-Path $root 'src/Native'
$output=Join-Path $root 'artifacts/native'
$objects=Join-Path $output 'obj'
New-Item -ItemType Directory -Path $objects -Force | Out-Null
$environment=Join-Path $installation 'VC/Auxiliary/Build/vcvars64.bat'
$engine='Engine.cpp','Inflate.cpp','Formats.cpp','Vector.cpp','Pdf.cpp' | ForEach-Object {'"'+(Join-Path $source $_)+'"'}
$flags='/nologo /O2 /W4 /EHsc /MT /std:c++17 /permissive- /utf-8 /guard:cf /Gy /DUNICODE /D_UNICODE /D_WIN32_WINNT=0x0A00 /DWINVER=0x0A00'
$libs='ole32.lib oleaut32.lib uuid.lib shlwapi.lib shell32.lib user32.lib gdi32.lib msimg32.lib windowscodecs.lib d2d1.lib dwrite.lib shcore.lib windowsapp.lib'
$link='/DYNAMICBASE /NXCOMPAT /CETCOMPAT /GUARD:CF /OPT:REF /OPT:ICF'
# Fixed compiler arguments and independently quoted paths; this script only builds.
$dll='call "'+$environment+'" >nul && cl '+$flags+' /LD '+($engine -join ' ')+' "'+(Join-Path $source 'Shell.cpp')+'" /Fo"'+$objects+'\\" /link '+$libs+' '+$link+' /DEF:"'+(Join-Path $source 'Shell.def')+'" /OUT:"'+$output+'\Whatsinthebox.Preview.dll"'
& $env:ComSpec /d /s /c $dll
if($LASTEXITCODE -ne 0){throw 'Native Explorer handler compilation failed'}
$tool='call "'+$environment+'" >nul && cl '+$flags+' '+($engine -join ' ')+' "'+(Join-Path $source 'RenderTool.cpp')+'" /Fo"'+$objects+'\\" /link '+$libs+' '+$link+' /SUBSYSTEM:CONSOLE /OUT:"'+$output+'\Whatsinthebox.Render.exe"'
& $env:ComSpec /d /s /c $tool
if($LASTEXITCODE -ne 0){throw 'Native render tool compilation failed'}
Write-Output "Native handlers: $output"
