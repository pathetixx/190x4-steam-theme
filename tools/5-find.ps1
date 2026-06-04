# ============================================================================
# 190x4 - locate Millennium data + our theme across the whole user profile,
# read which theme is active, and dump the REAL Millennium log.
# Output: find.txt on Desktop (send it back).
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\5-find.ps1"
# ============================================================================
$ErrorActionPreference = 'Continue'
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'find.txt'
$lines = @()
function L($s){ $script:lines += $s; Write-Host $s }

$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'

$roots = @(
  $env:USERPROFILE,
  $env:APPDATA,
  $env:LOCALAPPDATA,
  $env:PROGRAMDATA,
  $steam
) | Select-Object -Unique | Where-Object { $_ -and (Test-Path $_) }

L "Steam: $steam"
L "Search roots: $($roots -join ' ; ')"
L ""

# 1. every skin.json anywhere in those roots
L "=== ALL skin.json found ==="
$skins = @()
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -Filter 'skin.json' -File -EA SilentlyContinue -Depth 6 | ForEach-Object {
    $skins += $_.FullName
    $name = '?'; $ver = '?'
    try { $j = Get-Content $_.FullName -Raw -EA SilentlyContinue | ConvertFrom-Json; $name = $j.name; $ver = $j.version } catch {}
    L (" {0}`n      name={1} version={2}" -f $_.FullName, $name, $ver)
  }
}
if (-not $skins) { L " (none)" }
L ""

# 2. any millennium dir / config / log
L "=== millennium folders ==="
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -Directory -EA SilentlyContinue -Depth 4 |
    Where-Object { $_.Name -match 'millennium' } | ForEach-Object { L (" {0}" -f $_.FullName) }
}
L ""

L "=== millennium config / settings (active theme) ==="
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -File -EA SilentlyContinue -Depth 5 |
    Where-Object { $_.Name -match '^(config|settings|millennium).*\.json$' -and $_.FullName -match 'millennium' } |
    Select-Object -First 10 | ForEach-Object {
      L " --- $($_.FullName) ---"
      try { L (Get-Content $_.FullName -Raw -EA SilentlyContinue) } catch {}
      L ""
    }
}
L ""

L "=== millennium logs (last 50 lines each) ==="
foreach ($r in $roots) {
  Get-ChildItem -Path $r -Recurse -File -EA SilentlyContinue -Depth 5 |
    Where-Object { $_.FullName -match 'millennium' -and $_.Extension -match '\.(log|txt)$' } |
    Sort-Object LastWriteTime -Desc | Select-Object -First 4 | ForEach-Object {
      L " --- $($_.FullName) ---"
      Get-Content $_.FullName -Tail 50 -EA SilentlyContinue | ForEach-Object { L "   $_" }
      L ""
    }
}

$lines | Out-File -Encoding utf8 $out
Write-Host ""
Write-Host "Saved: $out  (send it back)"
Read-Host "Press Enter to close"
