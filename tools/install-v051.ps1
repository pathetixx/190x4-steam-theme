# ============================================================================
# 190x4 - (re)install theme v0.5.1 (CSS-only, no runtime JS) from the repo into
# Millennium's themes folder, then restart Steam.
# Run AS ADMINISTRATOR (writes into Program Files):
#   powershell -ExecutionPolicy Bypass -File "$env:USERPROFILE\Desktop\install-v051.ps1"
# ============================================================================
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'
$dest  = Join-Path $steam 'millennium\themes\190x4'
$base  = 'https://raw.githubusercontent.com/pathetixx/190x4-steam-theme/main'

$files = @(
  'skin.json',
  'theme/colors.css',
  'client/client.css',
  'webkit/store.css'
)

Write-Host "Installing 190x4 v0.5.1 -> $dest"
foreach ($f in $files) {
  $target = Join-Path $dest ($f -replace '/','\')
  $dir = Split-Path $target -Parent
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
  Invoke-WebRequest -Uri "$base/$f" -OutFile $target -UseBasicParsing
  Write-Host ("  ok  {0}" -f $f)
}

# remove the old runtime resolver if it lingers (we no longer inject it)
$res = Join-Path $dest 'client\resolver.js'
if (Test-Path $res) { Remove-Item $res -Force; Write-Host "  removed stale client/resolver.js" }

Write-Host ""
Write-Host "Restarting Steam..."
Get-Process steam, steamwebhelper -EA SilentlyContinue | Stop-Process -Force
Start-Sleep 3
Start-Process (Join-Path $steam 'steam.exe')

Write-Host ""
Write-Host "Done. Wait for the library to load."
Write-Host "If Steam DOES start: check scrollbar (red), Play button (red neon)."
Write-Host "If Steam does NOT start: delete the theme folder to recover:"
Write-Host "   $dest"
Read-Host "Press Enter to close"
