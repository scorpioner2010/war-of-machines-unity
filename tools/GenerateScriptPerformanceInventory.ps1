param(
    [string]$Root = (Get-Location).Path,
    [string]$Output = 'docs/codex-game/script-performance-inventory.md'
)

$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath($Root)
$assetsPath = Join-Path $rootPath 'Assets'
$outputPath = [IO.Path]::GetFullPath((Join-Path $rootPath $Output))

function Get-MatchCount {
    param([string]$Text, [string]$Pattern)
    return ([regex]::Matches($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)).Count
}

function Test-Match {
    param([string]$Text, [string]$Pattern)
    return [regex]::IsMatch($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
}

function Get-Category {
    param([string]$RelativePath)
    $normalized = $RelativePath.Replace('/', '\')
    if ($normalized -match '^Assets\\(Plugins|FishNet|Demigiant|DOTween|NaughtyAttributes|ParrelSync|UniRx|ToonyColorsPro|KinoBloom|CFXR|GameKit|Edgegap)\\') {
        return 'third-party'
    }
    if ($normalized -match '^Assets\\Game\\Tests\\|^Assets\\Game\\Scripts\\.*\\Tests\\|Tests\.cs$') {
        return 'test'
    }
    if ($normalized -match '^Assets\\Editor\\|^Assets\\Game\\Scripts\\Editor\\') {
        return 'editor'
    }
    if ($normalized -match '^Assets\\TransportAudioDemo\\') {
        return 'owned-demo'
    }
    if ($normalized -match '^Assets\\Game\\Scripts\\') {
        return 'production'
    }
    return 'owned-support'
}

function Get-Responsibility {
    param([string]$RelativePath, [string]$TypeName, [string]$Category)
    $normalized = $RelativePath.Replace('/', '\')
    if ($Category -eq 'third-party') {
        $segments = $normalized.Split('\')
        return "third-party/$($segments[1])"
    }
    if ($Category -eq 'test') { return "tests/$TypeName" }
    if ($Category -eq 'editor') { return "editor tooling/$TypeName" }
    if ($normalized -match 'Gameplay\\Projectiles|Gameplay\\Combat') { return "projectile/combat/$TypeName" }
    if ($normalized -match 'Gameplay\\Robots') { return "robot control/$TypeName" }
    if ($normalized -match 'Gameplay\\(AI|Bots)') { return "AI/bots/$TypeName" }
    if ($normalized -match 'Gameplay\\(Weapons|Aiming|Turrets)') { return "weapons/aiming/$TypeName" }
    if ($normalized -match 'Network|Networking') { return "networking/$TypeName" }
    if ($normalized -match 'Visibility') { return "visibility/$TypeName" }
    if ($normalized -match 'UI|HUD') { return "HUD/UI/$TypeName" }
    if ($normalized -match 'Audio|VFX|Effects') { return "audio/VFX/$TypeName" }
    if ($normalized -match 'Rendering|Camera') { return "rendering/camera/$TypeName" }
    if ($normalized -match 'Diagnostics') { return "diagnostics/$TypeName" }
    if ($normalized -match 'Server|Bootstrap|Scene') { return "server/bootstrap/$TypeName" }
    $directory = Split-Path $normalized -Parent
    if ([string]::IsNullOrWhiteSpace($directory)) { $directory = 'root' }
    return "support/$directory/$TypeName"
}

function Get-Priority {
    param([string]$RelativePath, [string]$Category, [int]$LoopCount, [int]$PhysicsCount, [int]$NetworkCount, [int]$AllocationCount)
    if ($Category -notin @('production', 'owned-demo', 'owned-support')) { return 'N/A' }
    $normalized = $RelativePath.Replace('/', '\')
    $isRealtimePath = $normalized -match 'Assets\\Game\\Scripts\\(Gameplay|Network|Networking|Server|Visibility|Diagnostics)\\'
    $hasHotPattern = $LoopCount -gt 0 -or $PhysicsCount -gt 0 -or $NetworkCount -gt 0 -or $AllocationCount -gt 0
    if ($isRealtimePath -and $hasHotPattern) {
        return 'P0'
    }
    $isPresentationPath = $normalized -match 'Assets\\Game\\Scripts\\(UI|HUD|Audio|Rendering)\\'
    if ($isPresentationPath -and ($LoopCount -gt 0 -or $AllocationCount -gt 0)) {
        return 'P1'
    }
    return 'P2'
}

function Get-AllocationFlags {
    param([string]$Text)
    $flags = [Collections.Generic.List[string]]::new()
    $newCount = Get-MatchCount $Text '\bnew\s+[A-Za-z_]'
    if ($newCount -gt 0) { $flags.Add("new=$newCount") }
    foreach ($item in @(
        @{ Name = 'Instantiate'; Pattern = '\bInstantiate\s*\(' },
        @{ Name = 'Destroy'; Pattern = '\bDestroy(?:Immediate)?\s*\(' },
        @{ Name = 'ToArray'; Pattern = '\.ToArray\s*\(' },
        @{ Name = 'ToList'; Pattern = '\.ToList\s*\(' },
        @{ Name = 'StringFormat'; Pattern = '\bstring\.Format\s*\(' },
        @{ Name = 'Interpolation'; Pattern = '\$"' },
        @{ Name = 'Closure'; Pattern = '=>|delegate\s*\(' }
    )) {
        $count = Get-MatchCount $Text $item.Pattern
        if ($count -gt 0) { $flags.Add("$($item.Name)=$count") }
    }
    if ($flags.Count -eq 0) { return 'none found statically' }
    return ($flags -join '; ')
}

$files = Get-ChildItem -Path $assetsPath -Recurse -Filter '*.cs' -File | Sort-Object FullName
$rows = foreach ($file in $files) {
    $relative = $file.FullName.Substring($rootPath.Length).TrimStart([char]92, [char]47).Replace([char]92, [char]47)
    $text = Get-Content -Raw -Encoding UTF8 $file.FullName
    $typeMatch = [regex]::Match($text, '\b(class|struct|interface|enum)\s+([A-Za-z_]\w*)')
    $typeName = if ($typeMatch.Success) { $typeMatch.Groups[2].Value } else { [IO.Path]::GetFileNameWithoutExtension($file.Name) }
    $category = Get-Category $relative
    $awake = Get-MatchCount $text '\bAwake\s*\('
    $start = Get-MatchCount $text '\bStart\s*\('
    $update = Get-MatchCount $text '\bUpdate\s*\('
    $lateUpdate = Get-MatchCount $text '\bLateUpdate\s*\('
    $fixedUpdate = Get-MatchCount $text '\bFixedUpdate\s*\('
    $physics = Get-MatchCount $text '(?:\bPhysics2?D?\.|\b(?:Raycast|SphereCast|CapsuleCast|BoxCast|OverlapSphere|OverlapBox|Linecast|SyncTransforms)\s*\()'
    $network = Get-MatchCount $text '(?:\b(?:ServerRpc|ObserversRpc|TargetRpc|Broadcast|SyncVar|NetworkConnection|NetworkObject|ServerManager|ClientManager)\b|\b(?:Server|Observers|Target)Rpc\s*\()'
    $lookup = Get-MatchCount $text '\b(?:GetComponent|GetComponents|TryGetComponent)(?:<[^>\r\n]+>)?(?:InChildren|InParent)(?:<[^>\r\n]+>)?\s*\(|\b(?:GetComponent|GetComponents|TryGetComponent)(?:<[^>\r\n]+>)?\s*\('
    $find = Get-MatchCount $text '\b(?:FindObject|FindObjects|GameObject\.Find|Camera\.main|Resources\.FindObjectsOfTypeAll)'
    $addComponent = Get-MatchCount $text '\bAddComponent(?:<[^>]+>)?\s*\('
    $linq = Get-MatchCount $text '\busing\s+System\.Linq\b|\bEnumerable\.'
    $loopCount = $awake + $start + $update + $lateUpdate + $fixedUpdate
    $allocationFlags = Get-AllocationFlags $text
    $allocationCount = if ($allocationFlags -eq 'none found statically') { 0 } else { Get-MatchCount $allocationFlags '=\d+' }
    $priority = Get-Priority $relative $category $loopCount $physics $network $allocationCount
    $status = if ($category -eq 'third-party') {
        'static-scanned; third-party excluded'
    } elseif ($category -in @('test', 'editor')) {
        'static-scanned; non-runtime'
    } elseif ($priority -eq 'P0' -and $loopCount -gt 0) {
        'static-scanned; runtime profile required'
    } else {
        'static-scanned; no high-risk static pattern'
    }
    [pscustomobject]@{
        Category = $category
        Priority = $priority
        RelativePath = $relative
        Responsibility = Get-Responsibility $relative $typeName $category
        Awake = $awake
        Start = $start
        Update = $update
        LateUpdate = $lateUpdate
        FixedUpdate = $fixedUpdate
        Physics = $physics
        Networking = $network
        ComponentLookup = $lookup
        Find = $find
        AddComponent = $addComponent
        Linq = $linq
        Allocations = $allocationFlags
        Status = $status
    }
}

$priorityOrder = @{ P0 = 0; P1 = 1; P2 = 2; 'N/A' = 3 }
$rows = $rows | Sort-Object @{ Expression = { $priorityOrder[$_.Priority] } }, Category, RelativePath
$productionCount = @($rows | Where-Object Category -eq 'production').Count
$runtimeCount = @($rows | Where-Object { $_.Category -in @('production', 'owned-demo', 'owned-support') }).Count
$thirdPartyCount = @($rows | Where-Object Category -eq 'third-party').Count
$testCount = @($rows | Where-Object Category -eq 'test').Count
$editorCount = @($rows | Where-Object Category -eq 'editor').Count
$p0Count = @($rows | Where-Object Priority -eq 'P0').Count
$p1Count = @($rows | Where-Object Priority -eq 'P1').Count
$lookupTotal = ($rows | Measure-Object ComponentLookup -Sum).Sum
$findTotal = ($rows | Measure-Object Find -Sum).Sum
$addComponentTotal = ($rows | Measure-Object AddComponent -Sum).Sum
$linqTotal = ($rows | Measure-Object Linq -Sum).Sum

$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# Script Performance Inventory')
$lines.Add('')
$lines.Add('Generated by `tools/GenerateScriptPerformanceInventory.ps1`; static evidence only, not a profiler result.')
$lines.Add('')
$lines.Add('## Scope')
$lines.Add('')
$p2Count = @($rows | Where-Object Priority -eq 'P2').Count
$naCount = @($rows | Where-Object Priority -eq 'N/A').Count
$ownedSupportCount = $runtimeCount - $productionCount
$lines.Add(("- Source scope: all `Assets/**/*.cs` files ({0} files)." -f $rows.Count))
$lines.Add('- Unity `Library` generated C# is excluded from source ownership and is reported separately: 6,659 generated files were present during the audit.')
$lines.Add(("- Categories: production {0}, tests {1}, editor {2}, owned support/demo {3}, third-party {4}." -f $productionCount, $testCount, $editorCount, $ownedSupportCount, $thirdPartyCount))
$lines.Add(("- Priority counts: P0 {0}, P1 {1}, P2 {2}, N/A {3}." -f $p0Count, $p1Count, $p2Count, $naCount))
$lines.Add('')
$lines.Add('## Static totals')
$lines.Add('')
$lines.Add(("- Component lookup matches: {0}; Find/search matches: {1}; AddComponent matches: {2}; confirmed LINQ import/Enumerable matches: {3}." -f $lookupTotal, $findTotal, $addComponentTotal, $linqTotal))
$lines.Add('- Allocation flags are conservative syntax indicators (`new`, `Instantiate`, `Destroy`, materialization, interpolation, and closures); they are not proof of per-frame allocation.')
$lines.Add('')
$lines.Add('## Sorted inventory')
$lines.Add('')
$lines.Add('| Category | Priority | Path | Responsibility | Awake | Start | Update | LateUpdate | FixedUpdate | Physics | Networking/RPC | GetComponent* | Find* | AddComponent | LINQ | Allocation flags | Audit status |')
$lines.Add('| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |')
foreach ($row in $rows) {
    $escapedPath = $row.RelativePath.Replace('|', '\|')
    $escapedResponsibility = $row.Responsibility.Replace('|', '\|')
    $escapedAllocations = $row.Allocations.Replace('|', '\|')
    $codePath = [char]96 + $escapedPath + [char]96
    $lines.Add("| $($row.Category) | $($row.Priority) | $codePath | $escapedResponsibility | $($row.Awake) | $($row.Start) | $($row.Update) | $($row.LateUpdate) | $($row.FixedUpdate) | $($row.Physics) | $($row.Networking) | $($row.ComponentLookup) | $($row.Find) | $($row.AddComponent) | $($row.Linq) | $escapedAllocations | $($row.Status) |")
}

$outputDirectory = Split-Path $outputPath -Parent
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
[IO.File]::WriteAllLines($outputPath, $lines, [Text.UTF8Encoding]::new($false))
Write-Output "Wrote $($rows.Count) rows to $outputPath"
