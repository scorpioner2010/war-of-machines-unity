param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $CliArgs
)

$ErrorActionPreference = "Stop"

function Split-RawCommandLine {
    param([string] $Raw)
    $items = @()
    if ([string]::IsNullOrWhiteSpace($Raw)) {
        return $items
    }

    $matches = [regex]::Matches($Raw, '("(?:[^"\\]|\\.)*"|''(?:[^'']|'''')*''|\S+)')
    foreach ($match in $matches) {
        $value = $match.Value
        if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2).Replace('\"', '"')
        }
        elseif ($value.Length -ge 2 -and $value.StartsWith("'") -and $value.EndsWith("'")) {
            $value = $value.Substring(1, $value.Length - 2).Replace("''", "'")
        }

        $items += $value
    }

    return $items
}

if (($null -eq $CliArgs -or $CliArgs.Count -eq 0) -and ![string]::IsNullOrWhiteSpace($env:GAME_DIAG_ARGS)) {
    $CliArgs = @(Split-RawCommandLine -Raw $env:GAME_DIAG_ARGS)
}

$Script:Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$Script:ExplicitBaseUrl = if ($env:GAME_DIAG_URL) { $env:GAME_DIAG_URL.TrimEnd("/") } else { $null }
$Script:BaseUrl = if ($Script:ExplicitBaseUrl) { $Script:ExplicitBaseUrl } else { "http://127.0.0.1:8765" }
$Script:FirstPort = 8765
$Script:LastPort = 8774
$Script:Token = $env:DIAGNOSTICS_TOKEN
$Script:EndpointInfos = $null

function Write-Err {
    param([string] $Message)
    [Console]::Error.WriteLine($Message)
}

function Get-ArgValue {
    param([Alias("Args")][string[]] $InputArgs, [string] $Name, [string] $Default)
    for ($i = 0; $i -lt $InputArgs.Count - 1; $i++) {
        if ($InputArgs[$i] -eq $Name) {
            return $InputArgs[$i + 1]
        }
    }

    return $Default
}

function Get-BaseUrlCandidates {
    if ($Script:ExplicitBaseUrl) {
        return @($Script:ExplicitBaseUrl)
    }

    $urls = @()
    for ($port = $Script:FirstPort; $port -le $Script:LastPort; $port++) {
        $urls += "http://127.0.0.1:$port"
    }

    return $urls
}

function Get-BaseUrlDescription {
    if ($Script:ExplicitBaseUrl) {
        return $Script:ExplicitBaseUrl
    }

    return "http://127.0.0.1:$Script:FirstPort-$Script:LastPort"
}

function Convert-DiagNumber {
    param([object] $Value)
    if ($null -eq $Value) {
        return 0.0
    }

    try {
        return [double]$Value
    }
    catch {
        return 0.0
    }
}

function Get-EndpointInfos {
    if ($null -ne $Script:EndpointInfos) {
        return $Script:EndpointInfos
    }

    $infos = @()
    foreach ($baseUrl in Get-BaseUrlCandidates) {
        try {
            $headers = @{}
            if ($Script:Token) {
                $headers["X-Diagnostics-Token"] = $Script:Token
            }

            $response = Invoke-WebRequest -Uri "$baseUrl/diagnostics/current" -Headers $headers -UseBasicParsing -TimeoutSec 1
            $current = $response.Content | ConvertFrom-Json
            $hasServer = $false
            $hasClient = $false
            if ($current.server) {
                $hasServer = $current.server.serverTickMs -ne $null -or $current.server.activePlayers -ne $null
            }
            if ($current.client) {
                $hasClient = $current.client.fps -ne $null -or $current.client.frameMs -ne $null
            }

            $infos += [pscustomobject]@{
                BaseUrl = $baseUrl
                Mode = $current.mode
                Map = $current.map
                HasServer = $hasServer
                HasClient = $hasClient
                ClientFrameP95 = if ($current.client) { Convert-DiagNumber $current.client.frameMsP95_10s } else { 0.0 }
                ClientFrameMax = if ($current.client) { Convert-DiagNumber $current.client.frameMsMax_10s } else { 0.0 }
                ServerTickP95 = if ($current.server) { Convert-DiagNumber $current.server.serverTickMsP95_10s } else { 0.0 }
                PingMs = if ($current.network) { Convert-DiagNumber $current.network.pingMs } else { 0.0 }
                JitterMs = if ($current.network) { Convert-DiagNumber $current.network.jitterMs } else { 0.0 }
                PacketLossPercent = if ($current.network) { Convert-DiagNumber $current.network.packetLossPercent } else { 0.0 }
                IncomingMessagesPerSecond = if ($current.network) { Convert-DiagNumber $current.network.incomingMessagesPerSecond } else { 0.0 }
            }
        }
        catch {
        }
    }

    $Script:EndpointInfos = $infos
    return $infos
}

function Select-BaseUrl {
    param([string] $PreferredGroup)
    if ($Script:ExplicitBaseUrl) {
        return $Script:ExplicitBaseUrl
    }

    $infos = @(Get-EndpointInfos)
    if ($infos.Count -eq 0) {
        return $Script:BaseUrl
    }

    if ($PreferredGroup -eq "server") {
        foreach ($info in $infos) {
            if ($info.HasServer -or $info.Mode -eq "server" -or $info.Mode -eq "client-server") {
                return $info.BaseUrl
            }
        }
    }

    if ($PreferredGroup -eq "client") {
        foreach ($info in $infos) {
            if ($info.Mode -eq "client" -or $info.Mode -eq "client-server") {
                return $info.BaseUrl
            }
        }
    }

    if ($PreferredGroup -eq "editor") {
        foreach ($info in $infos) {
            if ($info.Mode -eq "client" -or $info.Mode -eq "client-server") {
                return $info.BaseUrl
            }
        }
    }

    if ($PreferredGroup -eq "network") {
        $best = $infos[0]
        $bestScore = -1.0
        foreach ($info in $infos) {
            $score = $info.PingMs + ($info.JitterMs * 2.0) + ($info.PacketLossPercent * 100.0) + ([Math]::Min($info.IncomingMessagesPerSecond, 1000.0) / 10.0)
            if ($score -gt $bestScore) {
                $bestScore = $score
                $best = $info
            }
        }

        return $best.BaseUrl
    }

    if ($PreferredGroup -eq "analyze") {
        $best = $infos[0]
        $bestScore = -1.0
        foreach ($info in $infos) {
            $score = $info.ClientFrameP95 + ($info.ClientFrameMax / 4.0) + ($info.ServerTickP95 * 2.0) + $info.PingMs + ($info.JitterMs * 2.0) + ($info.PacketLossPercent * 100.0)
            if ($score -gt $bestScore) {
                $bestScore = $score
                $best = $info
            }
        }

        return $best.BaseUrl
    }

    return $infos[0].BaseUrl
}

function Invoke-DiagApi {
    param([string] $Path, [string] $PreferredGroup = $null)
    $headers = @{}
    if ($Script:Token) {
        $headers["X-Diagnostics-Token"] = $Script:Token
    }

    $lastError = $null
    $candidates = @(Select-BaseUrl -PreferredGroup $PreferredGroup)
    foreach ($baseUrl in Get-BaseUrlCandidates) {
        if ($candidates -notcontains $baseUrl) {
            $candidates += $baseUrl
        }
    }

    foreach ($baseUrl in $candidates) {
        $url = "$baseUrl$Path"
        try {
            $response = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 2
            $Script:BaseUrl = $baseUrl
            return $response.Content
        }
        catch {
            $lastError = $_.Exception
        }
    }

    if ($lastError) {
        throw $lastError
    }

    throw "diagnostics API unavailable"
}

function Invoke-OptionalDiagJson {
    param([string] $Path, [string] $PreferredGroup = $null, [object] $Fallback)
    try {
        return Invoke-DiagApi -Path $Path -PreferredGroup $PreferredGroup | ConvertFrom-Json
    }
    catch {
        return $Fallback
    }
}

function Get-LatestLogPath {
    $logDir = Join-Path $Script:Root "diagnostics\logs"
    if (!(Test-Path $logDir)) {
        return $null
    }

    $file = Get-ChildItem -Path $logDir -Filter "session-*.jsonl" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $file) {
        return $null
    }

    return $file.FullName
}

function Read-JsonlEvents {
    param([string] $Path)
    $events = @()
    if (!(Test-Path $Path)) {
        return $events
    }

    Get-Content -Path $Path -Tail 5000 | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace($_)) {
            return
        }

        try {
            $events += ($_ | ConvertFrom-Json)
        }
        catch {
        }
    }

    return $events
}

function Get-FallbackEvents {
    $path = Get-LatestLogPath
    if (!$path) {
        return [pscustomobject]@{ Path = $null; Events = @() }
    }

    return [pscustomobject]@{ Path = $path; Events = @(Read-JsonlEvents -Path $path) }
}

function Get-LatestMetricSample {
    param([object[]] $Events)
    for ($i = $Events.Count - 1; $i -ge 0; $i--) {
        if ($Events[$i].type -eq "metric_sample") {
            return $Events[$i].sample
        }
    }

    return $null
}

function Test-TimestampWithinSeconds {
    param([object] $Timestamp, [int] $Seconds)
    if ($null -eq $Timestamp) {
        return $true
    }

    try {
        $parsed = [DateTime]::Parse([string]$Timestamp, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal).ToUniversalTime()
        return $parsed -ge [DateTime]::UtcNow.AddSeconds(-[Math]::Max(1, $Seconds))
    }
    catch {
        return $true
    }
}

function Convert-AnalysisToText {
    param([object] $Analysis)
    if ($null -eq $Analysis) {
        return "classification: UNKNOWN`nsummary: diagnostics unavailable"
    }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("classification: $($Analysis.classification)")
    $lines.Add("severity: $($Analysis.severity)")
    $lines.Add("confidence: $($Analysis.confidence)")
    $lines.Add("summary: $($Analysis.summary)")
    if ($Analysis.evidence) {
        $lines.Add("evidence:")
        foreach ($item in $Analysis.evidence) {
            $lines.Add("- $item")
        }
    }
    if ($Analysis.topSuspects) {
        $lines.Add("top suspects:")
        foreach ($item in $Analysis.topSuspects) {
            $lines.Add("- $($item.name) [$($item.category)] avg=$($item.avgMs)ms max=$($item.maxMs)ms file=$($item.fileHint)")
        }
    }
    if ($Analysis.recommendedNextSteps) {
        $lines.Add("recommended next steps:")
        foreach ($item in $Analysis.recommendedNextSteps) {
            $lines.Add("- $item")
        }
    }

    return ($lines -join [Environment]::NewLine)
}

function Update-AnalysisForEditorFrameSpike {
    param([object] $Analysis)
    if ($null -eq $Analysis -or $Analysis.classification -ne "UNKNOWN") {
        return $Analysis
    }

    try {
        $current = Invoke-DiagApi -Path "/diagnostics/current" -PreferredGroup "analyze" | ConvertFrom-Json
        $serverCurrent = $null
        try {
            $serverCurrent = Invoke-DiagApi -Path "/diagnostics/current" -PreferredGroup "server" | ConvertFrom-Json
        }
        catch {
            $serverCurrent = $null
        }

        if ($null -eq $current -or $null -eq $current.client) {
            return $Analysis
        }

        $client = $current.client
        $server = if ($serverCurrent -and $serverCurrent.server) { $serverCurrent.server } else { $current.server }
        $frameMax = Convert-DiagNumber $client.frameMsMax_10s
        $frameP95 = Convert-DiagNumber $client.frameMsP95_10s
        $fps = Convert-DiagNumber $client.fps
        $screenWidth = Convert-DiagNumber $client.screenWidth
        $screenHeight = Convert-DiagNumber $client.screenHeight
        $isEditor = $client.isEditor -eq $true
        $focused = $true
        if ($null -ne $client.applicationFocused) {
            $focused = $client.applicationFocused -eq $true
        }
        $terrainActive = $false
        if ($client.terrain -and $client.terrain.activeTerrainPresent -eq $true) {
            $terrainActive = $true
        }

        $serverBad = $false
        $serverTickP95 = $null
        if ($server -and $server.serverTickMsP95_10s -ne $null) {
            $serverTickP95 = Convert-DiagNumber $server.serverTickMsP95_10s
            if ($serverTickP95 -gt 50) {
                $serverBad = $true
            }
        }

        $highResolution = $screenWidth -ge 1920 -or $screenHeight -ge 1080
        if ($frameMax -le 100 -or !$highResolution -or !$focused -or !$isEditor -or $serverBad) {
            return $Analysis
        }

        $evidence = New-Object System.Collections.Generic.List[string]
        $evidence.Add("client frameMs max is $frameMax ms")
        $evidence.Add("client frameMs p95 is $frameP95 ms")
        $evidence.Add("client FPS is $fps")
        $evidence.Add("application focused is $focused")
        $evidence.Add("screen is $($client.screenWidth)x$($client.screenHeight)")
        $evidence.Add("isEditor is $isEditor")
        if ($client.terrain) {
            $evidence.Add("active Terrain present is $($client.terrain.activeTerrainPresent)")
            if ($client.terrain.activeTerrainName) {
                $evidence.Add("active Terrain is $($client.terrain.activeTerrainName)")
            }
            if ($client.terrain.detailObjectDistance -ne $null) {
                $evidence.Add("terrain detail distance is $($client.terrain.detailObjectDistance)")
            }
            if ($client.terrain.treeDistance -ne $null) {
                $evidence.Add("terrain tree distance is $($client.terrain.treeDistance)")
            }
            if ($client.terrain.heightmapPixelError -ne $null) {
                $evidence.Add("terrain heightmap pixel error is $($client.terrain.heightmapPixelError)")
            }
        }
        if ($serverTickP95 -ne $null) {
            $evidence.Add("server tick p95 is $serverTickP95 ms")
        }
        if ($current.network) {
            $evidence.Add("ping is $($current.network.pingMs)ms")
            $evidence.Add("jitter is $($current.network.jitterMs)ms")
            $evidence.Add("packet loss is $($current.network.packetLossPercent)%")
        }

        $classification = "CLIENT_EDITOR_BOUND"
        $summary = "Focused high-resolution Unity Editor client has severe frame spikes while server tick is not the primary signal. This points to client Editor/render/frame pacing/debug UI/focus-dependent work."
        $nextSteps = @(
            "Repeat the A/B/C focus test and export diagnostics during the visible stutter.",
            "Disable Game View Gizmos/Stats/debug overlays/HUD, then re-run game-diag analyze --last 30.",
            "Check frame pacing settings: vSyncCount, targetFrameRate, Game View scale, Maximize On Play, and runInBackground.",
            "Do not patch server simulation first; current evidence points at the focused client Editor path."
        )
        if ($terrainActive) {
            $classification = "CLIENT_TERRAIN_EDITOR_RENDER_BOUND"
            $summary = "Focused high-resolution Unity Editor client has severe frame spikes while Terrain is active and server tick is not the primary signal. This points to Terrain/Game View rendering or Terrain editor overhead, not multiplayer sync."
            $nextSteps = @(
                "Toggle only the Terrain object and compare frame-spikes with the same focused 4K client.",
                "Test Terrain drawTreesAndFoliage, detailObjectDistance, treeDistance, heightmapPixelError, shadows, and Game View Gizmos/Stats before editing network code.",
                "If Development Build is smooth while Editor is bad, keep the fix in Editor/quality/Terrain settings instead of gameplay sync.",
                "Do not patch server simulation first; current evidence points at the focused client Terrain render path."
            )
        }

        return [pscustomobject]@{
            classification = $classification
            confidence = 0.82
            severity = "high"
            summary = $summary
            evidence = @($evidence)
            topSuspects = $Analysis.topSuspects
            recommendedNextSteps = $nextSteps
            filesToInspect = $Analysis.filesToInspect
        }
    }
    catch {
        return $Analysis
    }
}

function New-FallbackAnalysis {
    param([object] $Sample)
    if ($null -eq $Sample) {
        return [pscustomobject]@{
            classification = "UNKNOWN"
            confidence = 0
            severity = "low"
            summary = "No diagnostics API or JSONL metric_sample is available."
            evidence = @()
            topSuspects = @()
            recommendedNextSteps = @("Start the game with ENABLE_DIAGNOSTICS=true and run game-diag health.")
            filesToInspect = @()
        }
    }

    $clientBad = $false
    $serverBad = $false
    $networkBad = $false
    if ($Sample.client.frameMsP95_10s -ne $null -and [double]$Sample.client.frameMsP95_10s -gt 50) {
        $clientBad = $true
    }
    if ($Sample.client.fps -ne $null -and [double]$Sample.client.fps -lt 30) {
        $clientBad = $true
    }
    if ($Sample.server.serverTickMsP95_10s -ne $null -and [double]$Sample.server.serverTickMsP95_10s -gt 50) {
        $serverBad = $true
    }
    if ($Sample.network.pingMs -ne $null -and [double]$Sample.network.pingMs -gt 150) {
        $networkBad = $true
    }
    if ($Sample.network.jitterMs -ne $null -and [double]$Sample.network.jitterMs -gt 50) {
        $networkBad = $true
    }
    if ($Sample.network.packetLossPercent -ne $null -and [double]$Sample.network.packetLossPercent -gt 2) {
        $networkBad = $true
    }

    $classification = "UNKNOWN"
    $summary = "Fallback analysis from latest JSONL sample."
    $severity = "low"
    $confidence = 0.45
    if ($serverBad) {
        $classification = "SERVER_BOUND"
        $summary = "Server tick is above threshold in the latest JSONL sample."
        $severity = "high"
        $confidence = 0.7
    }
    elseif ($clientBad -and !$networkBad) {
        $classification = "CLIENT_BOUND"
        $summary = "Client frame time/FPS is unhealthy while network is not the primary signal."
        $severity = "high"
        $confidence = 0.7
    }
    elseif ($networkBad -and !$clientBad -and !$serverBad) {
        $classification = "NETWORK_BOUND"
        $summary = "Network ping/jitter/loss is unhealthy."
        $severity = "high"
        $confidence = 0.7
    }

    $evidence = @(
        "client frameMs p95 is $($Sample.client.frameMsP95_10s)ms",
        "client FPS is $($Sample.client.fps)",
        "server tick p95 is $($Sample.server.serverTickMsP95_10s)ms",
        "ping is $($Sample.network.pingMs)ms",
        "jitter is $($Sample.network.jitterMs)ms",
        "packet loss is $($Sample.network.packetLossPercent)%"
    )

    return [pscustomobject]@{
        classification = $classification
        confidence = $confidence
        severity = $severity
        summary = $summary
        evidence = $evidence
        topSuspects = @()
        recommendedNextSteps = @("Run against the live API during the lag window for top scopes: game-diag analyze --last 30.")
        filesToInspect = @()
    }
}

function Get-FallbackFrameSpikesJson {
    param([int] $Seconds)
    $fallback = Get-FallbackEvents
    $events = $fallback.Events
    $spikes = @()
    foreach ($event in $events) {
        if ($event.type -eq "frame_spike") {
            if (Test-TimestampWithinSeconds -Timestamp $event.timestamp -Seconds $Seconds) {
                $spikes += $event.frameSpike
            }
        }
        elseif ($event.type -eq "spike" -and $event.spike.type -eq "client_frame_spike") {
            if (Test-TimestampWithinSeconds -Timestamp $event.timestamp -Seconds $Seconds) {
                $spikes += $event.spike
            }
        }
    }

    return [pscustomobject]@{ seconds = $Seconds; source = "jsonl"; frameSpikes = $spikes }
}

function Output-ApiOrFallback {
    param([string] $Path, [scriptblock] $Fallback, [string] $PreferredGroup = $null)
    try {
        Invoke-DiagApi -Path $Path -PreferredGroup $PreferredGroup
        exit 0
    }
    catch {
        Write-Err "diagnostics API unavailable at $(Get-BaseUrlDescription); trying latest JSONL log."
        & $Fallback
    }
}

$command = if ($CliArgs.Count -gt 0) { $CliArgs[0] } else { "help" }
$last = [int](Get-ArgValue -Args $CliArgs -Name "--last" -Default "10")

try {
    if ($command -eq "health") {
        try {
            Invoke-DiagApi -Path "/diagnostics/health"
            exit 0
        }
        catch {
            Write-Err "diagnostics unavailable at $(Get-BaseUrlDescription). Start the game in Unity Editor, a Development Build, or with ENABLE_DIAGNOSTICS=true."
            exit 1
        }
    }

    if ($command -eq "current") {
        Output-ApiOrFallback -Path "/diagnostics/current" -Fallback {
            $fallback = Get-FallbackEvents
            $sample = Get-LatestMetricSample -Events $fallback.Events
            if ($sample) {
                $sample | ConvertTo-Json -Depth 20
                exit 0
            }

            Write-Err "game not running and no JSONL metric_sample found."
            exit 2
        } -PreferredGroup "analyze"
    }

    if ($command -eq "snapshot") {
        Output-ApiOrFallback -Path "/diagnostics/last?seconds=$last" -Fallback {
            $fallback = Get-FallbackEvents
            $events = $fallback.Events
            $samples = @()
            foreach ($event in $events) {
                if ($event.type -eq "metric_sample") {
                    $samples += $event.sample
                }
            }
            [pscustomobject]@{ seconds = $last; source = "jsonl"; samples = $samples } | ConvertTo-Json -Depth 20
            exit 0
        } -PreferredGroup "analyze"
    }

    if ($command -eq "spikes") {
        Output-ApiOrFallback -Path "/diagnostics/spikes?seconds=$last" -Fallback {
            $fallback = Get-FallbackEvents
            $events = $fallback.Events
            $spikes = @()
            foreach ($event in $events) {
                if ($event.type -eq "spike") {
                    $spikes += $event.spike
                }
            }
            [pscustomobject]@{ seconds = $last; source = "jsonl"; spikes = $spikes } | ConvertTo-Json -Depth 20
            exit 0
        } -PreferredGroup "analyze"
    }

    if ($command -eq "frame-spikes") {
        Output-ApiOrFallback -Path "/diagnostics/frame-spikes?seconds=$last" -Fallback {
            Get-FallbackFrameSpikesJson -Seconds $last | ConvertTo-Json -Depth 20
            exit 0
        } -PreferredGroup "analyze"
    }

    if ($command -eq "top") {
        if ($CliArgs.Count -lt 2 -or ($CliArgs[1] -ne "client" -and $CliArgs[1] -ne "server" -and $CliArgs[1] -ne "editor")) {
            Write-Err "usage: game-diag top client --last 10 | game-diag top server --last 10 | game-diag top editor --last 10"
            exit 3
        }

        $group = $CliArgs[1]
        Output-ApiOrFallback -Path "/diagnostics/top/$group`?seconds=$last" -PreferredGroup $group -Fallback {
            Write-Err "top scopes require the live diagnostics API."
            exit 1
        }
    }

    if ($command -eq "network") {
        Output-ApiOrFallback -Path "/diagnostics/network?seconds=$last" -PreferredGroup "network" -Fallback {
            $fallback = Get-FallbackEvents
            $sample = Get-LatestMetricSample -Events $fallback.Events
            if ($sample) {
                [pscustomobject]@{ seconds = $last; source = "jsonl"; network = $sample.network } | ConvertTo-Json -Depth 20
                exit 0
            }

            Write-Err "game not running and no JSONL network sample found."
            exit 2
        }
    }

    if ($command -eq "analyze") {
        try {
            $json = Invoke-DiagApi -Path "/diagnostics/analyze?seconds=$last" -PreferredGroup "analyze"
            $analysis = $json | ConvertFrom-Json
            $analysis = Update-AnalysisForEditorFrameSpike -Analysis $analysis
            $json = $analysis | ConvertTo-Json -Depth 20
            Convert-AnalysisToText -Analysis $analysis
            "json:"
            $json
            if ($analysis.severity -eq "high" -or $analysis.severity -eq "critical") {
                exit 4
            }

            exit 0
        }
        catch {
            Write-Err "diagnostics API unavailable at $(Get-BaseUrlDescription); trying latest JSONL log."
            $fallback = Get-FallbackEvents
            $sample = Get-LatestMetricSample -Events $fallback.Events
            $analysis = New-FallbackAnalysis -Sample $sample
            Convert-AnalysisToText -Analysis $analysis
            "json:"
            $analysis | ConvertTo-Json -Depth 20
            if ($analysis.severity -eq "high" -or $analysis.severity -eq "critical") {
                exit 4
            }

            if ($sample) {
                exit 0
            }

            exit 1
        }
    }

    if ($command -eq "export") {
        $out = Get-ArgValue -Args $CliArgs -Name "--out" -Default "diagnostics-report.json"
        try {
            $report = [pscustomobject]@{
                health = (Invoke-DiagApi -Path "/diagnostics/health" | ConvertFrom-Json)
                current = (Invoke-DiagApi -Path "/diagnostics/current" -PreferredGroup "analyze" | ConvertFrom-Json)
                samples = (Invoke-DiagApi -Path "/diagnostics/last?seconds=$last" -PreferredGroup "analyze" | ConvertFrom-Json)
                spikes = (Invoke-DiagApi -Path "/diagnostics/spikes?seconds=$last" -PreferredGroup "analyze" | ConvertFrom-Json)
                frameSpikes = (Invoke-OptionalDiagJson -Path "/diagnostics/frame-spikes?seconds=$last" -PreferredGroup "analyze" -Fallback (Get-FallbackFrameSpikesJson -Seconds $last))
                topClient = (Invoke-DiagApi -Path "/diagnostics/top/client?seconds=$last" -PreferredGroup "client" | ConvertFrom-Json)
                topServer = (Invoke-DiagApi -Path "/diagnostics/top/server?seconds=$last" -PreferredGroup "server" | ConvertFrom-Json)
                topEditor = (Invoke-DiagApi -Path "/diagnostics/top/editor?seconds=$last" -PreferredGroup "editor" | ConvertFrom-Json)
                network = (Invoke-DiagApi -Path "/diagnostics/network?seconds=$last" -PreferredGroup "network" | ConvertFrom-Json)
                analysis = (Update-AnalysisForEditorFrameSpike -Analysis (Invoke-DiagApi -Path "/diagnostics/analyze?seconds=$last" -PreferredGroup "analyze" | ConvertFrom-Json))
            }
            $report | ConvertTo-Json -Depth 30 | Set-Content -Path $out -Encoding UTF8
            "exported $out"
            exit 0
        }
        catch {
            Write-Err "export failed: $($_.Exception.Message)"
            exit 1
        }
    }

    "game-diag commands:"
    "  health"
    "  current"
    "  snapshot --last 10"
    "  spikes --last 30"
    "  frame-spikes --last 60"
    "  top client --last 10"
    "  top server --last 10"
    "  top editor --last 10"
    "  network --last 10"
    "  analyze --last 10"
    "  export --last 60 --out diagnostics-report.json"
    exit 0
}
catch {
    Write-Err "invalid response or command failure: $($_.Exception.Message)"
    exit 3
}
