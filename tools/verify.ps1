$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$required = @(
    "AGENTS.md",
    "Assets/Scripts/Core/DataCatalog.cs",
    "Assets/Prefabs/PF_Player_An.prefab",
    "Assets/Prefabs/PF_Player_Khoa.prefab",
    "Assets/Scenes/Farm_Main.unity",
    "Assets/Scenes/CombatSandbox.unity",
    "Docs/art-bible/ART-BIBLE.md",
    "Docs/art-bible/palette.gpl",
    ".github/CODEOWNERS",
    ".github/workflows/domain-tests.yml"
)

foreach ($path in $required) {
    if (-not (Test-Path (Join-Path $root $path))) {
        throw "Thieu file tuan 1: $path"
    }
}

dotnet run --project (Join-Path $root "Tests/PigTycoon.Runner.csproj")
if ($LASTEXITCODE -ne 0) {
    throw "Test core that bai"
}
