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
dotnet list (Join-Path $root 'Whatsinthebox.slnx') package --vulnerable --include-transitive --format json > (Join-Path $results 'dependencies.json')
if($LASTEXITCODE -ne 0){throw 'Dependency audit failed'}
$audit=Get-Content (Join-Path $results 'dependencies.json') -Raw | ConvertFrom-Json
foreach($project in $audit.projects){foreach($framework in $project.frameworks){if(@($framework.topLevelPackages).Count -or @($framework.transitivePackages).Count){throw 'Vulnerable dependencies reported'}}}
Write-Output "Passed: $($report.results.Count) renderer checks; geometry and dependency checks. Windows UI/integration checks are separate."
