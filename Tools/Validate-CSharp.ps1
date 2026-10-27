param([string]$EditorRoot = 'C:/Program Files/Unity/Hub/Editor/6000.5.9f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $compiler = Get-ChildItem "$EditorRoot/Data/DotNetSdk/sdk" -Filter csc.dll -Recurse | Select-Object -First 1 -ExpandProperty FullName
    $scratch = Join-Path $projectRoot 'Temp/UIStageValidation'
    [IO.Directory]::CreateDirectory($scratch) | Out-Null
    foreach ($assembly in @('Assembly-CSharp','Assembly-CSharp-Editor')) {
        $template = Get-ChildItem Library/Bee/artifacts -Filter "$assembly.rsp" -Recurse | Select-Object -First 1
        if (-not $template) { throw "Unity compiler response file missing: $assembly" }
        $argsForCompiler = [Collections.Generic.List[string]]::new()
        foreach ($line in [IO.File]::ReadAllLines($template.FullName)) {
            if ($line -match '^".*\.cs"$|^[-/]out:|^[-/]refout:|^[-/]analyzer:|^[-/]additionalfile:') { continue }
            if ($assembly -eq 'Assembly-CSharp-Editor' -and $line -match '^[-/]r:.*Assembly-CSharp(\.ref)?\.dll') { continue }
            $argsForCompiler.Add($line)
        }
        $argsForCompiler.Add('-out:"' + (Join-Path $scratch "$assembly.dll") + '"')
        if ($assembly -eq 'Assembly-CSharp-Editor') { $argsForCompiler.Add('-r:"' + (Join-Path $scratch 'Assembly-CSharp.dll') + '"') }
        foreach ($source in Get-ChildItem Assets -Recurse -Filter '*.cs') {
            $relative = $source.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
            if ($relative.StartsWith('Assets/ai.meshy/')) { continue } # separately compiled asmdef
            $isEditor = $relative -match '/Editor/'
            if (($assembly -eq 'Assembly-CSharp-Editor') -eq $isEditor) { $argsForCompiler.Add('"' + $relative + '"') }
        }
        $response = Join-Path $scratch "$assembly.rsp"
        [IO.File]::WriteAllLines($response,$argsForCompiler)
        & "$EditorRoot/Data/DotNetSdk/dotnet.exe" $compiler "@$response"
        if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $assembly" }
    }
} finally { Pop-Location }
