# ============================================================================
# 190x4 - apply v0.5.0 skin.json (inject base CSS/JS via ".*" so it reaches
# SharedJSContext where the real UI + webpack live), then restart Steam.
# Run AS ADMINISTRATOR (writes into Program Files):
#   right-click PowerShell -> Run as administrator, then:
#   powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\apply-skin-v05.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'

$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'
$theme = Join-Path $steam 'millennium\themes\190x4'
$skin  = Join-Path $theme 'skin.json'
if (-not (Test-Path $theme)) { throw "theme folder not found: $theme" }

$json = @'
{
  "name": "190x4",
  "author": "190x4",
  "description": "Cyberpunk command-center theme - red neon over graphite. Deep Steam reskin.",
  "version": "0.5.0",

  "github": { "owner": "pathetixx", "repo_name": "190x4-steam-theme" },
  "tags": ["Dark", "Cyberpunk", "Red", "Customizable"],

  "RootColors": "theme/colors.css",
  "Steam-WebKit": "webkit/store.css",

  "Patches": [
    { "MatchRegexString": ".*", "TargetCss": "theme/colors.css" },
    { "MatchRegexString": ".*", "TargetCss": "client/client.css", "TargetJs": "client/resolver.js" },
    { "MatchRegexString": ".*SP Overlay:.*", "TargetCss": "webkit/store.css" }
  ]
}
'@

# backup + write (UTF-8 no BOM)
if (Test-Path $skin) { Copy-Item $skin "$skin.bak" -Force }
[IO.File]::WriteAllText($skin, $json, (New-Object Text.UTF8Encoding($false)))
Write-Host "Wrote: $skin"

# restart Steam so Millennium re-injects
Get-Process steam, steamwebhelper -EA SilentlyContinue | Stop-Process -Force
Start-Sleep 3
Start-Process (Join-Path $steam 'steam.exe')
Write-Host ""
Write-Host "Steam restarting. Wait for the library to load, then run x4-report.ps1 again."
Read-Host "Press Enter to close"
