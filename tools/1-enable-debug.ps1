# ============================================================================
# 190x4 · ШАГ 1 — включить CEF remote debugging и перезапустить Steam
# Запускать в PowerShell (обычной, прав админа не требуется).
# Millennium должен быть СНЯТ (иначе конфликт, как в прошлый раз).
# ============================================================================
$ErrorActionPreference = 'Stop'

# путь к Steam из реестра (надёжнее, чем хардкод)
$steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -EA SilentlyContinue).SteamPath
if (-not $steam) { $steam = 'C:\Program Files (x86)\Steam' }
$steam = $steam -replace '/','\'
$exe = Join-Path $steam 'steam.exe'
if (-not (Test-Path $exe)) { throw "steam.exe не найден в $steam — поправь путь вручную" }

Write-Host "Steam: $steam"

# 1. полностью закрыть Steam
Get-Process steam, steamwebhelper -EA SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3

# 2. включить отладку (штатный файл Valve)
New-Item -ItemType File -Path (Join-Path $steam '.cef-enable-remote-debugging') -Force | Out-Null
Write-Host "Файл .cef-enable-remote-debugging создан."

# 3. запустить Steam
Start-Process $exe
Write-Host ""
Write-Host ">>> Дождись, пока Steam ПОЛНОСТЬЮ загрузится (видна библиотека)."
Write-Host ">>> Затем запусти второй скрипт: 2-dump.ps1"
