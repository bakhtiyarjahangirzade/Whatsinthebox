$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$results=Join-Path $root 'artifacts\checks'
New-Item -ItemType Directory -Path $results -Force | Out-Null
dotnet build (Join-Path $root 'Whatsinthebox.slnx') -c Release -m:1
if($LASTEXITCODE -ne 0){throw 'Solution build failed'}
dotnet run --project (Join-Path $root 'tests\Core\CoreChecks.csproj') -c Release > (Join-Path $results 'geometry.json')
if($LASTEXITCODE -ne 0){throw 'Geometry checks failed'}
$exe=Join-Path $root 'artifacts\app\Whatsinthebox.exe'
if(!(Test-Path -LiteralPath $exe)){throw 'Run scripts/build.ps1 first'}
$process=Start-Process -FilePath $exe -ArgumentList '--verify-rendering',('"'+(Join-Path $results 'rendering.json')+'"') -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0){throw 'Rendering checks failed'}
$report=Get-Content (Join-Path $results 'rendering.json') -Raw | ConvertFrom-Json
if($report.failed -ne 0){throw 'Rendering report contains failures'}
# Native engine: every supported fixture decodes, keeps its aspect and fills the box.
$native=Join-Path $results 'native'
New-Item -ItemType Directory -Path $native -Force | Out-Null
$fixtures=Join-Path $native 'fixtures'
$process=Start-Process -FilePath $exe -ArgumentList '--write-explorer-fixtures',('"'+$fixtures+'"') -WindowStyle Hidden -PassThru -Wait
if($process.ExitCode -ne 0){throw 'Fixture generation failed'}
$render=Join-Path $root 'artifacts\native\Whatsinthebox.Render.exe'
$nativeRows=[Collections.Generic.List[object]]::new()
foreach($file in Get-ChildItem -LiteralPath $fixtures -File){
 if($file.Extension -notin '.psd','.svg','.pdf','.cdr','.ora','.tga','.ppm','.ttf'){continue}
 foreach($edge in 256,1024){
  $line=& $render $file.FullName (Join-Path $native ($file.BaseName+'-'+$file.Extension.TrimStart('.')+'-'+$edge+'.png')) $edge
  $row=$line | ConvertFrom-Json
  $expected=if($file.Extension -in '.pdf','.ttf'){$null}else{2.0}
  $aspect=$row.width/[double][Math]::Max(1,$row.height)
  $passed=($LASTEXITCODE -eq 0) -and ([Math]::Max($row.width,$row.height) -eq $edge) -and ($null -eq $expected -or [Math]::Abs($aspect-$expected) -lt 0.02)
  $nativeRows.Add(@{file=$file.Name;edge=$edge;passed=$passed;result=$row})
 }
}
$nativeRows | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $native 'native-render.json')
if(@($nativeRows | Where-Object {!$_.passed}).Count){throw 'Native engine checks failed; see native-render.json'}
dotnet list (Join-Path $root 'Whatsinthebox.slnx') package --vulnerable --include-transitive --format json > (Join-Path $results 'dependencies.json')
if($LASTEXITCODE -ne 0){throw 'Dependency audit failed'}
$audit=Get-Content (Join-Path $results 'dependencies.json') -Raw | ConvertFrom-Json
foreach($project in $audit.projects){foreach($framework in $project.frameworks){if(@($framework.topLevelPackages).Count -or @($framework.transitivePackages).Count){throw 'Vulnerable dependencies reported'}}}
Write-Output "Passed: $($report.results.Count) renderer checks, $($nativeRows.Count) native engine checks; geometry and dependency checks. Windows UI/integration checks are separate."
