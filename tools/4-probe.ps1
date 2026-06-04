# ============================================================================
# 190x4 - probe Millennium install: where our theme is, what theme is active,
# and what the Millennium log says about injection. No Steam needed running.
# Output: probe.txt on Desktop (send it back).
# Run: powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\4-probe.ps1"
# ============================================================================
$ErrorActionPreference = 'Continue'
$out = Join-Path ([Environment]::GetFolderPath('Desktop')) 'probe.txt'
$lines = @()
function L($s){ $script:lines += $s; Write-Host $s }

$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'
L "Steam: $steam"
L ""

# 1. where are all skin.json on disk (find our theme's real location)
L "=== skin.json files under Steam ==="
Get-ChildItem -Path $steam -Recurse -Filter 'skin.json' -EA SilentlyContinue | ForEach-Object {
  L (" {0}" -f $_.FullName)
  try {
    $j = Get-Content $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    L ("    name={0} version={1}" -f $j.name, $j.version)
  } catch { L "    (JSON parse FAILED: $($_.Exception.Message))" }
}
L ""

# 2. our theme folder contents (expected files present?)
$themesRoots = @(
  (Join-Path $steam 'millennium\themes'),
  (Join-Path $steam 'steamui\skins')
)
foreach ($tr in $themesRoots) {
  if (Test-Path $tr) {
    L "=== themes root: $tr ==="
    Get-ChildItem $tr -Directory -EA SilentlyContinue | ForEach-Object { L ("  [dir] {0}" -f $_.Name) }
    $ours = Join-Path $tr '190x4'
    if (Test-Path $ours) {
      L "  --- contents of 190x4 ---"
      Get-ChildItem $ours -Recurse -File -EA SilentlyContinue | ForEach-Object {
        L ("    {0}" -f $_.FullName.Substring($ours.Length+1))
      }
    }
    L ""
  }
}

# 3. Millennium config (which theme is active)
L "=== Millennium config / active theme ==="
$cfgCandidates = @(
  (Join-Path $steam 'ext\data\config.json'),
  (Join-Path $steam 'ext\data\settings.json'),
  (Join-Path $steam 'millennium\config.json'),
  "$env:USERPROFILE\.millennium\config.json"
)
Get-ChildItem -Path (Join-Path $steam 'ext') -Recurse -Filter '*.json' -EA SilentlyContinue |
  Select-Object -First 20 | ForEach-Object { $cfgCandidates += $_.FullName }
$cfgCandidates | Select-Object -Unique | ForEach-Object {
  if (Test-Path $_) {
    $txt = Get-Content $_ -Raw -EA SilentlyContinue
    if ($txt -match 'active|theme|190x4|skin') {
      L " --- $_ ---"
      L ($txt.Substring(0, [Math]::Min(800, $txt.Length)))
      L ""
    }
  }
}

# 4. Millennium version + logs
L "=== Millennium logs ==="
$logDirs = @(
  (Join-Path $steam 'ext\data\logs'),
  (Join-Path $steam 'millennium\logs'),
  (Join-Path $steam 'logs')
)
foreach ($ld in $logDirs) {
  if (Test-Path $ld) {
    L " logdir: $ld"
    Get-ChildItem $ld -File -EA SilentlyContinue | Sort-Object LastWriteTime -Desc | Select-Object -First 3 | ForEach-Object {
      L "  --- $($_.Name) (last 40 lines) ---"
      Get-Content $_.FullName -Tail 40 -EA SilentlyContinue | ForEach-Object { L "   $_" }
    }
  }
}

$lines | Out-File -Encoding utf8 $out
Write-Host ""
Write-Host "Saved: $out  (send it back)"
Read-Host "Press Enter to close"
