param(
    [string]$Root = (Get-Location).Path,
    [string]$Output = 'docs/codex-game/heavy-method-audit.md',
    [string]$OutputAll = 'docs/codex-game/heavy-method-audit-all.md'
)

$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
$assetsPath = Join-Path $rootPath 'Assets'
$outputPath = [IO.Path]::GetFullPath((Join-Path $rootPath $Output))
$outputAllPath = [IO.Path]::GetFullPath((Join-Path $rootPath $OutputAll))

function Get-Category {
    param([string]$RelativePath)
    $normalized = $RelativePath.Replace('/', '\')
    if ($normalized -match '^Assets\\(Plugins|FishNet|Demigiant|DOTween|NaughtyAttributes|ParrelSync|UniRx|ToonyColorsPro|KinoBloom|CFXR|GameKit|Edgegap|JMO Assets|Layer Lab|Particles111|Free UI Click Sound Effects Pack|TextMesh Pro|Toby Fredson)\\') { return 'third-party' }
    if ($normalized -match '^Assets\\Game\\Tests\\|^Assets\\Game\\Scripts\\(?:.*\\)?(?:Tests|Testing)\\|Tests\.cs$') { return 'test' }
    if ($normalized -match '^Assets\\Editor\\|^Assets\\Game\\Scripts\\Editor\\') { return 'editor' }
    if ($normalized -match '^Assets\\TransportAudioDemo\\') { return 'owned-demo' }
    if ($normalized -match '^Assets\\Game\\Scripts\\') { return 'production' }
    return 'owned-support'
}

function Get-MethodRows {
    param([IO.FileInfo]$File, [string]$RelativePath, [string]$Category)
    $lines = @(Get-Content -Encoding UTF8 $File.FullName)
    $methodStarts = [Collections.Generic.List[int]]::new()
    $methodNames = @{}
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        $trimmedLine = $line.TrimStart()
        if ($trimmedLine -match '^(return|throw|new|yield|case|default|else|do)\b') { continue }
        $match = [regex]::Match($line, '^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|unsafe|new|partial|ref|readonly)\s+)*(?:[A-Za-z_][\w<>\[\],.?]*\s+)+(?<name>[A-Za-z_]\w*)\s*\(')
        if ($match.Success -and $match.Groups['name'].Value -notin @('if', 'for', 'foreach', 'while', 'switch', 'catch', 'using', 'lock')) {
            $methodStarts.Add($index)
            $methodNames[$index] = $match.Groups['name'].Value
        }
    }

    $methodData = [Collections.Generic.List[object]]::new()
    for ($startIndex = 0; $startIndex -lt $methodStarts.Count; $startIndex++) {
        $start = $methodStarts[$startIndex]
        $end = if ($startIndex + 1 -lt $methodStarts.Count) { $methodStarts[$startIndex + 1] - 1 } else { $lines.Count - 1 }
        $signatureEnd = [Math]::Min($end, $start + 12)
        $signatureWindow = ($lines[$start..$signatureEnd] -join ' ')
        $signatureMatch = [regex]::Match(
            $signatureWindow,
            '^\s*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|unsafe|new|partial|ref|readonly)\s+)*(?:[A-Za-z_][\w<>\[\],.?]*\s+)+[A-Za-z_]\w*\s*\([^)]*\)\s*(?:where\s+[^<{]+\s*)?(?<tail>\{|=>|;)')
        if (!$signatureMatch.Success -or $signatureMatch.Groups['tail'].Value -eq ';') { continue }
        $methodData.Add([pscustomobject]@{
            Start = $start
            End = $end
            Name = $methodNames[$start]
            Body = ($lines[$start..$end] -join "`n")
        })
    }

    $hotMethodNames = [Collections.Generic.HashSet[string]]::new()
    foreach ($method in $methodData | Where-Object Name -match '^(Update|LateUpdate|FixedUpdate|OnGUI|Tick|OnPreTick|OnPostTick|NetworkTick)$') {
        $hotMethodNames.Add($method.Name) | Out-Null
    }

    $changed = $true
    while ($changed) {
        $changed = $false
        foreach ($caller in $methodData | Where-Object { $hotMethodNames.Contains($_.Name) }) {
            foreach ($callee in $methodData) {
                if ($caller.Name -eq $callee.Name) { continue }
                $isCall = [regex]::IsMatch($caller.Body, "\b$([regex]::Escape($callee.Name))\s*\(")
                if ($isCall -and $hotMethodNames.Add($callee.Name)) {
                    $changed = $true
                }
            }
        }
    }

    foreach ($method in $methodData) {
        $start = $method.Start
        $body = $method.Body
        $name = $method.Name
        $evidence = [Collections.Generic.List[string]]::new()
        $score = 0

        $patterns = @(
            @{ Name = 'Update/LateUpdate/FixedUpdate/OnGUI'; Pattern = '\b(?:Update|LateUpdate|FixedUpdate|OnGUI)\s*\('; Weight = 10 },
            @{ Name = 'physics query'; Pattern = '\b(?:Physics2?D?\.|Raycast|SphereCast|CapsuleCast|BoxCast|Linecast|OverlapSphere|OverlapBox|SyncTransforms)'; Weight = 8 },
            @{ Name = 'allocation'; Pattern = '\bnew\s+[A-Za-z_]|\bInstantiate\s*\(|\bDestroy(?:Immediate)?\s*\(|\.ToArray\s*\(|\.ToList\s*\(|\bstring\.Format\s*\(|\$"'; Weight = 6 },
            @{ Name = 'LINQ'; Pattern = '(?<!Mathf)\.(?:Where|Select|OrderBy|OrderByDescending|ThenBy|GroupBy|Join|First|FirstOrDefault|Single|SingleOrDefault|Any|All|ToLookup|ToDictionary|Distinct|Concat|Union|Except|Aggregate|Sum|Average|Min|Max)\s*\('; Weight = 7 },
            @{ Name = 'component/search lookup'; Pattern = 'GetComponent|GetComponents|TryGetComponent|FindFirstObjectByType|FindAnyObjectByType|FindObjectsByType|FindObjectOfType|FindObjectsOfType|Resources\.Find|GameObject\.Find|Camera\.main|AddComponent|Shader\.Find'; Weight = 6 },
            @{ Name = 'UI rebuild/activation'; Pattern = '\b(?:SetActive|ForceUpdateCanvases|LayoutRebuilder|CanvasUpdateRegistry|SetAllDirty|SetVerticesDirty|SetLayoutDirty)\s*\('; Weight = 5 },
            @{ Name = 'network/RPC'; Pattern = '\b(?:ServerRpc|ObserversRpc|TargetRpc|Broadcast|Send|NetworkConnection|ServerManager|ClientManager)\b'; Weight = 4 },
            @{ Name = 'loop'; Pattern = '\b(?:for|foreach|while)\s*\('; Weight = 3 },
            @{ Name = 'distance/trig'; Pattern = '\b(?:Mathf\.(?:Sin|Cos|Tan|Atan|Atan2)|Vector3\.Distance|Vector2\.Distance|\.magnitude\b)'; Weight = 3 },
            @{ Name = 'renderer/material'; Pattern = '\.(?:material|materials)\b|new\s+Material\s*\('; Weight = 4 },
            @{ Name = 'string/boxing risk'; Pattern = '\.ToString\s*\(|\$"|string\.Format|\bbox\b'; Weight = 2 }
        )

        foreach ($item in $patterns) {
            $count = ([regex]::Matches($body, $item.Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)).Count
            if ($count -gt 0) {
                $evidence.Add("$($item.Name)=$count")
                $score += $item.Weight * [Math]::Min($count, 4)
            }
        }

        if ($evidence.Count -eq 0) { continue }
        $isLifecycle = $name -match '^(Update|LateUpdate|FixedUpdate|OnGUI|Tick|OnPreTick|OnPostTick|NetworkTick)$'
        $calledFromHotMethod = $hotMethodNames.Contains($name)
        if ($isLifecycle -or $calledFromHotMethod) { $score += 8 }
        $priority = if ($Category -eq 'third-party' -or $Category -in @('test', 'editor')) { 'N/A' } elseif ($score -ge 28 -and ($isLifecycle -or $body -match 'Physics|Instantiate|Destroy|ToArray|ToList|Where\s*\(')) { 'P0' } elseif ($score -ge 18) { 'P1' } else { 'P2' }
        $frequency = if ($isLifecycle) { 'per-frame/tick candidate' } elseif ($calledFromHotMethod) { 'called by per-frame/tick candidate' } elseif ($body -match 'OnEnable|OnDisable|Awake|Start|Spawn|Despawn|Death|Hit|Damage|Rpc|Command|Callback|Event') { 'event/setup candidate' } else { 'call frequency unknown' }
        $risk = if ($Category -eq 'third-party') { 'third-party; do not modify mechanically' } elseif ($Category -in @('test', 'editor')) { 'non-runtime harness/tooling' } elseif ($priority -eq 'P0') { 'high; verify with profiler and call-count capture' } elseif ($priority -eq 'P1') { 'medium; profile before refactor' } else { 'low/unknown; no hot-path proof' }
        $relativeLine = $start + 1
        $methodPath = "`$RelativePath`:$relativeLine"
        [pscustomobject]@{
            Category = $Category
            Priority = $priority
            Score = $score
            Path = $RelativePath
            Line = $relativeLine
            Method = $name
            Frequency = $frequency
            Evidence = ($evidence -join ', ')
            Risk = $risk
        }
    }
}

$files = @(Get-ChildItem -Path $assetsPath -Recurse -Filter '*.cs' -File | Sort-Object FullName)
$rows = foreach ($file in $files) {
    $relative = $file.FullName.Substring($rootPath.Length).TrimStart([char]92, [char]47).Replace([char]92, [char]47)
    Get-MethodRows $file $relative (Get-Category $relative)
}

$priorityOrder = @{ P0 = 0; P1 = 1; P2 = 2; 'N/A' = 3 }
$rows = @($rows | Sort-Object @{ Expression = { $priorityOrder[$_.Priority] } }, @{ Expression = { -$_.Score } }, Category, Path, Line)
$ownedRows = @($rows | Where-Object Category -in @('production', 'owned-demo', 'owned-support'))
$productionRows = @($rows | Where-Object Category -eq 'production')
$p0 = @($rows | Where-Object Priority -eq 'P0')
$p1 = @($rows | Where-Object Priority -eq 'P1')
$physicsRows = @($rows | Where-Object Evidence -match 'physics query')
$allocationRows = @($rows | Where-Object Evidence -match 'allocation=|LINQ=')
$networkRows = @($rows | Where-Object Evidence -match 'network/RPC')
$uiRows = @($rows | Where-Object Evidence -match 'UI rebuild/activation')

$linesOut = [Collections.Generic.List[string]]::new()
$linesOut.Add('# Heavy Method Audit')
$linesOut.Add('')
$linesOut.Add('Generated by `tools/GenerateHeavyMethodAudit.ps1`; static candidate ranking only, not a profiler result.')
$linesOut.Add('')
$linesOut.Add('## Scope and interpretation')
$linesOut.Add('')
$linesOut.Add(("- Scanned all `{0}` C# files under `Assets/`; method candidates with heavy-operation evidence: {1}." -f $files.Count, $rows.Count))
$linesOut.Add(("- Owned production candidates: {0}; owned support/demo candidates: {1}; owned P0: {2}; owned P1: {3}." -f $productionRows.Count, @($ownedRows | Where-Object Category -in @('owned-demo', 'owned-support')).Count, @($ownedRows | Where-Object Priority -eq 'P0').Count, @($ownedRows | Where-Object Priority -eq 'P1').Count))
$linesOut.Add(("- Physics candidates: {0}; allocation/LINQ candidates: {1}; network/RPC candidates: {2}; UI rebuild/activation candidates: {3}." -f $physicsRows.Count, $allocationRows.Count, $networkRows.Count, $uiRows.Count))
$linesOut.Add('- A static candidate is not proof of cost: a method may run once, be pooled, execute only in a test scene, or use a non-alloc API.')
$linesOut.Add('- Priority is a review order: P0 means a hot-path or high-cost combination that needs matched profiling; it is not a measured performance ranking.')
$linesOut.Add('- `RaycastNonAlloc` and similar APIs avoid result-array GC but can still be expensive on the physics thread; `new Vector3`, `new Ray`, and other value-type construction are not managed allocations by themselves.')
$linesOut.Add('- Methods under `Assets/Game/Scripts/Testing`, editor folders, and imported packages are listed separately or excluded from the owned runtime ranking; they are not gameplay hot paths.')
$linesOut.Add('')
$linesOut.Add('## Highest-risk owned methods')
$linesOut.Add('')
$linesOut.Add('| Priority | Score | Category | Method | Path | Frequency candidate | Static evidence | Recommended next action |')
$linesOut.Add('| --- | ---: | --- | --- | --- | --- | --- | --- |')
foreach ($row in @($ownedRows | Where-Object Priority -in @('P0', 'P1')) | Select-Object -First 150) {
    $action = if ($row.Evidence -match 'physics query') { 'Capture physics time/hit count; preserve hit order before considering NonAlloc/batching.' } elseif ($row.Evidence -match 'LINQ|allocation') { 'Capture GC.Alloc and call frequency; replace only if confirmed hot.' } elseif ($row.Evidence -match 'UI rebuild') { 'Capture Canvas/layout rebuild markers; batch state changes before changing behavior.' } elseif ($row.Evidence -match 'network/RPC') { 'Capture tick/RPC count and payload size; do not alter authority/order mechanically.' } else { 'Profile call count and inclusive time before refactor.' }
    $linesOut.Add("| $($row.Priority) | $($row.Score) | $($row.Category) | ``$($row.Method)`` | ``$($row.Path):$($row.Line)`` | $($row.Frequency) | $($row.Evidence) | $action |")
}
$linesOut.Add('')
$linesOut.Add('## All owned candidates by category')
$linesOut.Add('')
foreach ($category in @('physics', 'allocations/LINQ', 'UI', 'networking', 'search/lookups', 'rendering/math')) {
    $categoryRows = switch ($category) {
        'physics' { @($ownedRows | Where-Object Evidence -match 'physics query') }
        'allocations/LINQ' { @($ownedRows | Where-Object Evidence -match 'allocation=|LINQ=') }
        'UI' { @($ownedRows | Where-Object Evidence -match 'UI rebuild/activation') }
        'networking' { @($ownedRows | Where-Object Evidence -match 'network/RPC') }
        'search/lookups' { @($ownedRows | Where-Object Evidence -match 'component/search lookup') }
        'rendering/math' { @($ownedRows | Where-Object Evidence -match 'renderer/material|distance/trig') }
    }
    $linesOut.Add("### $category")
    $linesOut.Add('')
    if ($categoryRows.Count -eq 0) {
        $linesOut.Add('- No owned production candidates matched the static pattern.')
    } else {
        $linesOut.Add('| Priority | Method | Path | Evidence | Frequency | Risk |')
        $linesOut.Add('| --- | --- | --- | --- | --- | --- |')
        foreach ($row in $categoryRows) {
            $linesOut.Add("| $($row.Priority) | ``$($row.Method)`` | ``$($row.Path):$($row.Line)`` | $($row.Evidence) | $($row.Frequency) | $($row.Risk) |")
        }
    }
    $linesOut.Add('')
}
$linesOut.Add('## Third-party, editor, and test candidates')
$linesOut.Add('')
$linesOut.Add('- The full static candidate list includes third-party, editor, and test rows in the generated source inventory. They are intentionally not mixed into the production remediation list.')
$linesOut.Add('- Third-party rows require upstream ownership and library-specific profiling; do not rewrite them mechanically.')
$linesOut.Add('- Editor/test rows can legitimately use global search, dynamic object creation, and component discovery because they inspect arbitrary assets or construct their fixtures.')
$linesOut.Add('')
$linesOut.Add('## Required profiler validation')
$linesOut.Add('')
$linesOut.Add('- Record matched call counts and inclusive time for each P0/P1 method in startup, movement, aiming/zoom, sustained fire, projectile impact, spectator cycling, and 10v9 battle.')
$linesOut.Add('- Record `GC.Alloc`, collection count, physics query count/time, Canvas/layout rebuilds, network tick/RPC count, active robots/projectiles/UI objects, and frame-time p95/max.')
$linesOut.Add('- Only after those captures should a candidate be changed or assigned a CPU/GC/physics percentage improvement.')

$outputDirectory = Split-Path $outputPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
[IO.File]::WriteAllLines($outputPath, $linesOut, [Text.UTF8Encoding]::new($false))

$allLines = [Collections.Generic.List[string]]::new()
$allLines.Add('# All Static Heavy-Method Candidates')
$allLines.Add('')
$allLines.Add('Generated by `tools/GenerateHeavyMethodAudit.ps1`; static candidates only, not profiler results.')
$allLines.Add('')
$allLines.Add(("- Complete method-level list across all 1,472 `Assets/**/*.cs` files: {0} candidates." -f $rows.Count))
$allLines.Add('- `production`, `owned-support`, and `owned-demo` are project-owned; `test`, `editor`, and `third-party` are classified for review but should not be mechanically modified.')
$allLines.Add('')
$allLines.Add('| Category | Priority | Score | Method | Path | Frequency candidate | Static evidence | Risk |')
$allLines.Add('| --- | --- | ---: | --- | --- | --- | --- | --- |')
foreach ($row in $rows) {
    $allLines.Add("| $($row.Category) | $($row.Priority) | $($row.Score) | ``$($row.Method)`` | ``$($row.Path):$($row.Line)`` | $($row.Frequency) | $($row.Evidence) | $($row.Risk) |")
}

$outputAllDirectory = Split-Path $outputAllPath -Parent
New-Item -ItemType Directory -Force -Path $outputAllDirectory | Out-Null
[IO.File]::WriteAllLines($outputAllPath, $allLines, [Text.UTF8Encoding]::new($false))
Write-Output "Wrote $($rows.Count) heavy-method candidates to $outputPath and $outputAllPath"
