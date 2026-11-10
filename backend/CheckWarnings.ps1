# Builds the back-end projects that contain changed or new .cs files and prints the compiler and analyzer
# warnings that are located in those files. Warnings in files nobody touched are not shown.
#
# Run from anywhere:  pwsh backend/CheckWarnings.ps1   (or: powershell -File backend/CheckWarnings.ps1)
# Exit code: 0 = no warnings in changed files, 1 = warnings found, 2 = a build failed.
# Changed means: differs from HEAD (staged or not) or is untracked, under backend/. Nothing is modified.

$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel).Trim()
$backendRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'backend'))

function ConvertTo-FullPath([string] $relativeToRepo) {
    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $relativeToRepo))
}

$changed = @(
    git -C $repoRoot -c core.safecrlf=false diff --name-only HEAD -- backend
    git -C $repoRoot -c core.safecrlf=false ls-files --others --exclude-standard -- backend
) |
    Where-Object { $_ -and $_.EndsWith('.cs') } |
    ForEach-Object { ConvertTo-FullPath $_ } |
    Where-Object { Test-Path -LiteralPath $_ } |
    Sort-Object -Unique

if ($changed.Count -eq 0) {
    Write-Host 'No changed .cs files under backend/.'
    exit 0
}

# The nearest .csproj above each changed file, but never above backend/
$projects = @()
foreach ($file in $changed) {
    $directory = Split-Path -Parent $file
    while ($directory.StartsWith($backendRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        $project = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' -File | Select-Object -First 1
        if ($project) {
            $projects += $project.FullName
            break
        }
        $directory = Split-Path -Parent $directory
    }
}
$projects = $projects | Sort-Object -Unique

$pattern = '^(?<file>.+?)\((?<line>\d+),(?<column>\d+)\): (?<kind>warning|error) (?<code>[A-Za-z]+\d+): (?<message>.*?)(?: \[(?<project>[^\]]+)\])?$'
$found = @{}
$buildFailed = $false

foreach ($project in $projects) {
    Write-Host "Building $([System.IO.Path]::GetFileName($project)) ..."
    $output = & dotnet build $project --no-incremental -nologo -v:q
    if ($LASTEXITCODE -ne 0) {
        $buildFailed = $true
    }

    foreach ($line in $output) {
        $match = [regex]::Match($line.Trim(), $pattern)
        if (-not $match.Success) {
            continue
        }

        $file = $match.Groups['file'].Value
        if (-not [System.IO.Path]::IsPathRooted($file)) {
            $file = Join-Path (Split-Path -Parent $project) $file
        }
        $file = [System.IO.Path]::GetFullPath($file)

        if ($changed -notcontains $file) {
            continue
        }

        $key = "$file|$($match.Groups['line'].Value)|$($match.Groups['column'].Value)|$($match.Groups['code'].Value)"
        $found[$key] = [pscustomobject]@{
            File    = $file.Substring($repoRoot.Length).TrimStart('\', '/')
            Line    = [int] $match.Groups['line'].Value
            Column  = [int] $match.Groups['column'].Value
            Kind    = $match.Groups['kind'].Value
            Code    = $match.Groups['code'].Value
            Message = $match.Groups['message'].Value
        }
    }
}

$results = $found.Values | Sort-Object File, Line, Column
foreach ($result in $results) {
    Write-Host "$($result.File)($($result.Line),$($result.Column)): $($result.Kind) $($result.Code): $($result.Message)"
}

$fileCount = @($results | Select-Object -ExpandProperty File -Unique).Count
Write-Host ''
Write-Host "$(@($results).Count) warning(s) or error(s) in $fileCount of $($changed.Count) changed file(s), $($projects.Count) project(s) built."

if ($buildFailed) {
    Write-Host 'At least one build failed. Fix the errors first.'
    exit 2
}

exit $(if (@($results).Count -gt 0) { 1 } else { 0 })
